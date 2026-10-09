using BitShelter.Backup;
using BitShelter.Models;
using BitShelter.Service.Backup;
using BitShelter.Service.Data;
using BitShelter.VSS;
using Quartz;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BitShelter.Service.Jobs
{
  // Copies files from the snapshots that SnapshotJob just created. SnapshotJob puts the backup in BackupQueue
  // and schedules one trigger for it. Only one backup runs at a time: Quartz holds the other triggers back
  // without using a worker thread for each waiting backup.
  [DisallowConcurrentExecution]
  public class BackupJob : IJob
  {
    public static readonly JobKey Key = new JobKey("BackupJob");

    public long RuleId { get; set; }

    public static ITrigger CreateTrigger(long ruleId)
    {
      return TriggerBuilder.Create()
        .WithIdentity("BackupJob " + ruleId + " " + Guid.NewGuid(), "BackupJob")
        .ForJob(Key)
        .UsingJobData("RuleId", ruleId)
        .StartNow()
        .Build();
    }

    public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
      BackupQueue.Entry entry = BackupQueue.Instance.Take(RuleId);

      if (entry == null)
        return ValueTask.CompletedTask;

      try
      {
        Run(entry.SnapshotIds, entry.BackupNames, cancellationToken);
      }
      finally
      {
        BackupQueue.Instance.Finish(entry);
      }

      return ValueTask.CompletedTask;
    }

    private void Run(List<Guid> snapshotIds, HashSet<string> names, CancellationToken cancellationToken)
    {
      SnapshotRule rule = RuleMgr.Instance.GetRule(RuleId);

      if (rule == null || !rule.Enabled || !rule.BackupEnabled)
        return;

      try
      {
        List<VssSnapshotProperties> snapshots;

        using (var vss = new VssClient(new VssHost()))
        {
          vss.Initialize(VssSnapshotContextInternal.All);
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
