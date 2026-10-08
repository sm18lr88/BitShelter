using BitShelter.Models;
using BitShelter.Service.Backup;
using BitShelter.Service.Data;
using Serilog;
using System;
using System.Collections.Generic;

namespace BitShelter.Service.Ipc
{
  internal sealed class SnapshotService : ISnapshotService
  {
    public bool Ping() => true;

    public IEnumerable<SnapshotRule> GetRules()
    {
      return RuleMgr.Instance.Rules;
    }

    public bool AddOrUpdateRule(SnapshotRule rule)
    {
      try
      {
        RuleMgr.Instance.AddOrUpdateRule(rule);
        return true;
      }
      catch (Exception ex)
      {
        Log.Error(ex, "Failed to add/update rule {Id}", rule?.Id);
        return false;
      }
    }

    public IEnumerable<BackupResult> GetBackupResults(long sinceId)
    {
      return BackupStateMgr.Instance.GetResultsSince(sinceId);
    }

    public bool DeleteRule(SnapshotRule rule, bool deleteSnapshots)
    {
      try
      {
        return RuleMgr.Instance.DeleteRule(rule, deleteSnapshots);
      }
      catch (Exception ex)
      {
        Log.Error(ex, "Failed to delete rule {Id}", rule?.Id);
        return false;
      }
    }
  }
}
