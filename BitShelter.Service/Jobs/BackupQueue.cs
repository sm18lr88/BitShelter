using BitShelter.Service.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BitShelter.Service.Jobs
{
  // The backups that wait for BackupJob: at most one for each rule. If a rule takes a new snapshot before its
  // waiting backup starts, the waiting backup moves to the new snapshot and also runs the newly due backup rules.
  // So backups that are slower than the snapshot schedule do not pile up. The snapshots of a waiting or running
  // backup are pinned, so that pruning keeps them.
  internal sealed class BackupQueue
  {
    internal sealed class Entry
    {
      public List<Guid> SnapshotIds { get; set; }
      public HashSet<string> BackupNames { get; set; }
    }

    private readonly object gate = new object();
    private readonly Dictionary<long, Entry> waiting = new Dictionary<long, Entry>();
    private readonly Action<IEnumerable<Guid>> pin;
    private readonly Action<IEnumerable<Guid>> unpin;

    public static BackupQueue Instance { get; } = new BackupQueue(ids => PruningMgr.Instance.Pin(ids), ids => PruningMgr.Instance.Unpin(ids));

    internal BackupQueue(Action<IEnumerable<Guid>> pin, Action<IEnumerable<Guid>> unpin)
    {
      this.pin = pin;
      this.unpin = unpin;
    }

    // Returns true when the caller must schedule a BackupJob trigger for the rule, and false when a waiting
    // backup of the rule took the new snapshot.
    public bool Enqueue(long ruleId, IEnumerable<Guid> snapshotIds, IEnumerable<string> backupNames)
    {
      List<Guid> ids = snapshotIds.ToList();

      lock (gate)
      {
        pin(ids);

        if (waiting.TryGetValue(ruleId, out Entry entry))
        {
          unpin(entry.SnapshotIds);
          entry.SnapshotIds = ids;
          entry.BackupNames.UnionWith(backupNames);
          return false;
        }

        waiting[ruleId] = new Entry { SnapshotIds = ids, BackupNames = new HashSet<string>(backupNames) };
        return true;
      }
    }

    // Undoes Enqueue when the trigger could not be scheduled.
    public void Cancel(long ruleId)
    {
      lock (gate)
      {
        if (waiting.Remove(ruleId, out Entry entry))
          unpin(entry.SnapshotIds);
      }
    }

    // Starts the waiting backup of the rule. Returns null if there is none. Call Finish when the backup ends.
    public Entry Take(long ruleId)
    {
      lock (gate)
      {
        return waiting.Remove(ruleId, out Entry entry) ? entry : null;
      }
    }

    public void Finish(Entry entry)
    {
      unpin(entry.SnapshotIds);
    }
  }
}
