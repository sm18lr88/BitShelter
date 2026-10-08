using Alphaleonis.Win32.Vss;
using BitShelter.Backup;
using BitShelter.Models;
using BitShelter.Service.Backup;
using BitShelter.Service.Data;
using BitShelter.VSS;
using Newtonsoft.Json;
using Quartz;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BitShelter.Service.Jobs
{
  // Copies files from the snapshots that SnapshotJob just created. SnapshotJob starts one BackupJob
  // with the backup rules that are due. Only one backup runs at a time.
  public class BackupJob : IJob
  {
    private static readonly SemaphoreSlim OneAtATime = new SemaphoreSlim(1, 1);

    public long RuleId { get; set; }
    // JSON arrays, because Quartz job data holds simple values.
    public string SnapshotIds { get; set; }
    public string BackupNames { get; set; }

    public static IJobDetail Create(long ruleId, IEnumerable<Guid> snapshotIds, IEnumerable<string> backupNames)
    {
      return JobBuilder.Create<BackupJob>()
        .WithIdentity("BackupJob " + ruleId + " " + Guid.NewGuid())
        .UsingJobData("RuleId", ruleId)
        .UsingJobData("SnapshotIds", JsonConvert.SerializeObject(snapshotIds))
        .UsingJobData("BackupNames", JsonConvert.SerializeObject(backupNames))
        .Build();
    }

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
      await OneAtATime.WaitAsync(cancellationToken).ConfigureAwait(false);

      try
      {
        Run(cancellationToken);
      }
      finally
      {
        OneAtATime.Release();
      }
    }

    private void Run(CancellationToken cancellationToken)
    {
      SnapshotRule rule = RuleMgr.Instance.GetRule(RuleId);

      if (rule == null || !rule.Enabled || !rule.BackupEnabled)
        return;

      List<Guid> snapshotIds = JsonConvert.DeserializeObject<List<Guid>>(SnapshotIds ?? "[]");
      HashSet<string> names = JsonConvert.DeserializeObject<HashSet<string>>(BackupNames ?? "[]");

      PruningMgr.Instance.Pin(snapshotIds);

      try
      {
        List<VssSnapshotProperties> snapshots;

        using (var vss = new VssClient(new VssHost()))
        {
          vss.Initialize(VssSnapshotContext.All, VssBackupType.Incremental);
          snapshots = snapshotIds.Select(vss.GetSnapshotProperties).ToList();
        }

        foreach (BackupRule backup in rule.BackupRules.Where(b => names.Contains(b.Name)))
          Record(rule, backup, RunOne(rule, backup, snapshots, cancellationToken));
      }
      catch (Exception ex) when (ex is not OperationCanceledException)
      {
        Log.Error(ex, "Backups of rule {RuleName} failed", rule.Name);

        foreach (BackupRule backup in rule.BackupRules.Where(b => names.Contains(b.Name)))
          Record(rule, backup, Failed(rule, backup, DateTime.Now, ex));
      }
      finally
      {
        PruningMgr.Instance.Unpin(snapshotIds);
      }
    }

    private static BackupResult RunOne(SnapshotRule rule, BackupRule backup, IReadOnlyList<VssSnapshotProperties> snapshots, CancellationToken cancellationToken)
    {
      DateTime startedAt = DateTime.Now;

      try
      {
        List<BackupSource> sources = SnapshotBackupSources.Create(backup.InputFolders, snapshots);

        return BackupEngine.Run(rule, backup, sources, startedAt, cancellationToken);
      }
      catch (Exception ex) when (ex is not OperationCanceledException)
      {
        return Failed(rule, backup, startedAt, ex);
      }
    }

    private static BackupResult Failed(SnapshotRule rule, BackupRule backup, DateTime startedAt, Exception ex)
    {
      return new BackupResult
      {
        RuleId = rule.Id,
        RuleName = rule.Name,
        BackupName = backup.Name,
        StartedAt = startedAt,
        FinishedAt = DateTime.Now,
        Success = false,
        Message = ex.Message,
        Exception = ex,
      };
    }

    private static void Record(SnapshotRule rule, BackupRule backup, BackupResult result)
    {
      result.Notify = rule.BackupNotify && (result.Success ? backup.NotifyComplete : backup.NotifyError);

      if (result.Success)
        Log.Information("Backup {BackupName} of rule {RuleName} completed: {OutputPath}. {Message}", result.BackupName, result.RuleName, result.OutputPath, result.Message);
      else
        Log.Error(result.Exception, "Backup {BackupName} of rule {RuleName} failed: {Message}", result.BackupName, result.RuleName, result.Message);

      result.Exception = null;
      BackupStateMgr.Instance.AddResult(result);
    }
  }
}
