using BitShelter.Models;
using System.Collections.Generic;

namespace BitShelter.Service.Ipc
{
  internal interface ISnapshotService
  {
    bool Ping();
    IEnumerable<SnapshotRule> GetRules();
    bool AddOrUpdateRule(SnapshotRule rule);
    bool DeleteRule(SnapshotRule rule, bool deleteSnapshots);
    IEnumerable<BackupResult> GetBackupResults(long sinceId);
  }
}

