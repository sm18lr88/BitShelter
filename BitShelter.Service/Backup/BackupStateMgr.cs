using BitShelter.Models;
using BitShelter.Service.Config;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BitShelter.Service.Backup
{
  // Persists backups.json: the snapshot counter of each rule (for Every/Offset) and the latest backup results.
  public sealed class BackupStateMgr
  {
    public const string FileName = "backups.json";
    public const int MaxResults = 100;

    private readonly object sync = new object();
    private BackupState state = new BackupState();
    private string filePath;

    public static BackupStateMgr Instance { get; } = new BackupStateMgr();

    internal BackupStateMgr() { }

    public void Load(string folderPath)
    {
      lock (sync)
      {
        filePath = Path.Combine(folderPath, FileName);
        state = ConfigLoader.LoadTrustedJson<BackupState>(filePath).GetAwaiter().GetResult() ?? new BackupState();
      }
    }

    // Returns the number of the current snapshot of the rule (0 for the first one) and increments the counter.
    public long NextSnapshotNumber(long ruleId)
    {
      lock (sync)
      {
        state.SnapshotCounters.TryGetValue(ruleId, out long number);
        state.SnapshotCounters[ruleId] = number + 1;
        Save();

        return number;
      }
    }

    public void ForgetRule(long ruleId)
    {
      lock (sync)
      {
        if (state.SnapshotCounters.Remove(ruleId))
          Save();
      }
    }

    public BackupResult AddResult(BackupResult result)
    {
      lock (sync)
      {
        result.Id = ++state.LastResultId;
        state.Results.Add(result);

        if (state.Results.Count > MaxResults)
          state.Results.RemoveRange(0, state.Results.Count - MaxResults);

        Save();

        return result;
      }
    }

    public IReadOnlyList<BackupResult> GetResultsSince(long sinceId)
    {
      lock (sync)
        return state.Results.Where(r => r.Id > sinceId).ToList();
    }

    private void Save()
    {
      if (filePath != null)
        ConfigLoader.WriteJsonAtomic(filePath, state);
    }

    internal sealed class BackupState
    {
      public Dictionary<long, long> SnapshotCounters { get; set; } = new Dictionary<long, long>();
      public long LastResultId { get; set; }
      public List<BackupResult> Results { get; set; } = new List<BackupResult>();
    }
  }
}
