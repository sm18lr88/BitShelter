using BitShelter.Data;
using BitShelter.Models;
using BitShelter.Service.Config;
using BitShelter.Utils;
using BitShelter.VSS;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace BitShelter.Service.Data
{
  public class PruningMgr
  {
    private ConcurrentDictionary<long, List<SnapshotInstance>> SnapshotRuleInstanceMap { get; set; }
    private object _lockProcessing = new object();
    private object _lockSave = new object();
    // Snapshots that a running backup reads. Pruning skips them until the backup ends.
    private readonly ConcurrentDictionary<Guid, int> _pinned = new ConcurrentDictionary<Guid, int>();
    // Rules whose snapshots are deleted with the rule. A snapshot job that runs at that moment must not record
    // its new snapshots, because nothing would prune them after the rule is gone.
    private readonly HashSet<long> _deletedRules = new HashSet<long>();

    protected PruningMgr() { }

    protected static PruningMgr _instance = null;
    public static PruningMgr Instance => _instance ?? (_instance = new PruningMgr());

    public void LoadInstances(IEnumerable<SnapshotInstance> instances)
    {
      lock (_lockProcessing)
      {
        SnapshotRuleInstanceMap = new ConcurrentDictionary<long, List<SnapshotInstance>>();

        foreach (var instance in instances)
        {
          List<SnapshotInstance> ruleInstances = SafeGetInstances(instance.SnapshotRuleId);

          ruleInstances.Add(instance);
        }
      }
    }

    public void DeleteAllForRule(VssClient vss, SnapshotRule rule)
    {
      lock (_lockProcessing)
      {
        _deletedRules.Add(rule.Id);
        bool allDeleted = DeleteSnapshots(vss, SafeGetInstances(rule.Id).ToList());

        Save();

        if (!allDeleted)
          throw new InvalidOperationException($"Some snapshots of rule {rule.Id} could not be deleted.");
      }
    }

    public void DoPruning(VssClient vss)
    {
      lock (_lockProcessing)
      {
        CleanupGhostInstances(vss);

        DeleteSnapshots(vss, GetPruningList().ToList());

        Save();
      }
    }

    // A snapshot that fails to delete stays tracked, is retried on the next run, and does not block the others.
    private bool DeleteSnapshots(VssClient vss, IReadOnlyCollection<SnapshotInstance> instances)
    {
      var deleted = new HashSet<Guid>();

      foreach (SnapshotInstance instance in instances)
      {
        try
        {
          vss.DeleteSnapshot(instance.SnapshotId);
          deleted.Add(instance.SnapshotId);
        }
        catch (Exception ex)
        {
          Log.Warning(ex, "Failed to delete snapshot {SnapshotId} of rule {RuleId}", instance.SnapshotId, instance.SnapshotRuleId);
        }
      }

      CleanupInstances(deleted);

      return deleted.Count == instances.Count;
    }

    // Global pruning strategy. See PruningStrategy and VolumeLimitPruning.
    public void MakeRoomForRule(VssClient vss, SnapshotRule rule, int limit)
    {
      lock (_lockProcessing)
      {
        IEnumerable<ExistingSnapshot> existing = vss.QuerySnapshotSet()
          .Select(p => new ExistingSnapshot { SnapshotId = p.SnapshotId, VolumeName = p.OriginalVolumeName });
        IEnumerable<(Guid, DateTime)> owned = SafeGetInstances(rule.Id).Select(i => (i.SnapshotId, i.CreatedAt));

        IReadOnlyList<Guid> toDelete = VolumeLimitPruning.SelectToMakeRoom(existing, owned, rule.Volumes, limit, _pinned.Keys.ToHashSet(), out IReadOnlyList<string> stillFull);

        foreach (string volume in stillFull)
          Log.Warning("Volume {Volume} is at its limit of {Limit} snapshots and rule {RuleName} has no snapshot to delete there. VSS will delete the oldest snapshot of the volume", volume, limit, rule.Name);

        if (toDelete.Count == 0)
          return;

        Log.Information("Deleting {Count} old snapshots of rule {RuleName} to stay under the limit of {Limit} snapshots per volume", toDelete.Count, rule.Name, limit);
        DeleteSnapshots(vss, SafeGetInstances(rule.Id).Where(i => toDelete.Contains(i.SnapshotId)).ToList());
        Save();
      }
    }

    public void Pin(IEnumerable<Guid> snapshotIds)
    {
      foreach (Guid id in snapshotIds)
        _pinned.AddOrUpdate(id, 1, (_, count) => count + 1);
    }

    public void Unpin(IEnumerable<Guid> snapshotIds)
    {
      foreach (Guid id in snapshotIds)
        if (_pinned.AddOrUpdate(id, 0, (_, count) => count - 1) <= 0)
          _pinned.TryRemove(id, out _);
    }

    // Called when the deletion of a rule failed and the rule stays.
    public void KeepRule(long ruleId)
    {
      lock (_lockProcessing)
        _deletedRules.Remove(ruleId);
    }

    // Returns false, and records nothing, if the rule is being deleted with its snapshots.
    public bool TryCreateNewInstances(long ruleId, IEnumerable<Guid> snapshotIds)
    {
      lock (_lockProcessing)
      {
        if (_deletedRules.Contains(ruleId))
          return false;

        List<SnapshotInstance> ruleInstances = SafeGetInstances(ruleId);
        DateTime now = DateTime.Now;

        ruleInstances.AddRange(snapshotIds.Select(id => new SnapshotInstance()
        {
          SnapshotId = id,
          SnapshotRuleId = ruleId,
          CreatedAt = now
        }));

        Save();
        return true;
      }
    }

    private void Save()
    {
      lock (_lockSave)
      {
        List<SnapshotInstance> allInstances = SnapshotRuleInstanceMap.Values.SelectMany(i => i).ToList();

        ConfigLoader.SaveToFile(allInstances, ConfigLoader.SnapshotInstancesFileName);
      }
    }

    private List<SnapshotInstance> SafeGetInstances(long ruleId)
    {
      List<SnapshotInstance> ret = SnapshotRuleInstanceMap.SafeGet(
        ruleId,
        new List<SnapshotInstance>()
      );

      SnapshotRuleInstanceMap[ruleId] = ret;

      return ret;
    }

    private void CleanupInstances(HashSet<Guid> toRm)
    {
      foreach (List<SnapshotInstance> instances in SnapshotRuleInstanceMap.Values)
        instances.RemoveAll(i => toRm.Contains(i.SnapshotId));
    }

    private void CleanupGhostInstances(VssClient vss)
    {
      HashSet<Guid> existingSnapshots = vss.QuerySnapshotSet().Select(sp => sp.SnapshotId).ToHashSet();

      foreach (List<SnapshotInstance> instances in SnapshotRuleInstanceMap.Values)
        instances.RemoveAll(i => existingSnapshots.Contains(i.SnapshotId) == false);
    }

    private IEnumerable<SnapshotInstance> GetPruningList()
    {
      return RuleMgr.Instance.Rules.SelectMany(r => GetPruningListForRule(r));
    }

    private IEnumerable<SnapshotInstance> GetPruningListForRule(SnapshotRule rule)
    {
      List<SnapshotInstance> instances = SnapshotRuleInstanceMap.SafeGet(rule.Id);

      if (instances == null)
        return new List<SnapshotInstance>();

      long lifetime = rule.GetLifeTimeSeconds();

      return instances.Where(i => i.CreatedAt.AddSeconds(lifetime) < DateTime.Now && !_pinned.ContainsKey(i.SnapshotId)).ToList();
    }
  }
}
