using BitShelter.Backup;
using BitShelter.Models;
using BitShelter.Models.Enums;
using BitShelter.Service.Backup;
using BitShelter.Service.Data;
using BitShelter.Service.Scheduler;
using BitShelter.Utils;
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
  // https://www.quartz-scheduler.net/documentation/best-practices.html
  [DisallowConcurrentExecution]
  public class SnapshotJob : IJob
  {
    public long RuleId { get; set; }
    public int RetryCount { get; set; }

    public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
      VssClient vss = null;
      int maxRetryCount = 0;
      bool restartVss = false;

      try
      {
        SnapshotRule rule = RuleMgr.Instance.GetRule(RuleId);

        if (rule == null)
        {
          Log.Error("Failed to retrieve SnapshotRule {RuleId}", RuleId);

          throw new InvalidOperationException(String.Format("Failed to retrieve SnapshotRule {0}", RuleId));
        }

        maxRetryCount = rule.MaxRetryCount;
        restartVss = rule.RetryRestartVSSService;

        Log.Debug("Executing SnapshotJob for {RuleName}", rule.Name);

        if (rule.Enabled == false) // Shouldn't happen expect in rare cases
          return ValueTask.CompletedTask;

        if (rule.PruningStrategy == PruningStrategy.Global)
          MakeRoom(rule);

        vss = new VssClient(new VssHost());
        vss.Initialize(rule.SnapshotContext);

        List<Guid> snapshotIds = vss.CreateSnapshot(rule.Volumes).ToList();

        PruningMgr.Instance.CreateNewInstances(rule.Id, snapshotIds);

        if (rule.BackupEnabled && rule.BackupRules != null && rule.BackupRules.Count > 0)
          ScheduleDueBackups(context.Scheduler, rule, snapshotIds);

        Log.Debug("Completed SnapshotJob for {RuleId}", RuleId);
      }
      catch (Exception ex)
      {
        Log.Error(ex, "Failed SnapshotJob for {RuleId}", RuleId);

        Retry(ex, context.Scheduler, context.JobDetail, maxRetryCount, restartVss);

        return ValueTask.CompletedTask;
      }
      finally
      {
        if (vss != null)
        {
          vss.Dispose();
          vss = null;
        }
      }

      return ValueTask.CompletedTask;
    }

    private void ScheduleDueBackups(IScheduler scheduler, SnapshotRule rule, IReadOnlyCollection<Guid> snapshotIds)
    {
      long snapshotNumber = BackupStateMgr.Instance.NextSnapshotNumber(rule.Id);
      List<string> due = rule.BackupRules
        .Where(b => BackupSchedule.IsDue(snapshotNumber, b.Offset, b.Every))
        .Select(b => b.Name)
        .ToList();

      if (due.Count == 0)
        return;

      Log.Debug("Scheduling BackupJob for {RuleId}: {BackupNames}", RuleId, due);

      ITrigger trigger = TriggerBuilder.Create().StartNow().Build();

      scheduler.ScheduleJob(BackupJob.Create(rule.Id, snapshotIds, due), trigger).GetAwaiter().GetResult();
    }

    protected void Retry(Exception ex, IScheduler scheduler, IJobDetail jobDetail, int maxRetryCount, bool restartVss)
    {
      if (RetryCount < maxRetryCount)
      {
        if (restartVss)
          RestartVss();

        Log.Information("Retrying SnapshotJob for {RuleId} in 1 minute (attempt {RetryCount}/{MaxRetryCount})", RuleId, RetryCount, maxRetryCount);

        ITrigger trigger = TriggerBuilder.Create()
          .WithIdentity(RuleId + " Retry " + RetryCount, "SnapshotJob")
          .ForJob(jobDetail)
          .StartAt(DateTime.Now.AddMinutes(1))
          .UsingJobData("RuleId", RuleId)
          .UsingJobData("RetryCount", ++RetryCount)
          .Build();

        scheduler.ScheduleJob(trigger).GetAwaiter().GetResult();
      }

      else
        throw new JobExecutionException(ex);
    }

    // The registry limit (MaxShadowCopies) applies to client-accessible snapshots, which both supported contexts create.
    // Uses its own VSS session: in writer mode, a query on the session that then creates the snapshot makes
    // AddToSnapshotSet fail with VSS_E_UNEXPECTED (SetContextInternal in a bad state).
    private static void MakeRoom(SnapshotRule rule)
    {
      int limit = VssUtils.GetSnapshotLimit();

      try
      {
        using var vss = new VssClient(new VssHost());
        vss.Initialize(VssSnapshotContextInternal.All);
        PruningMgr.Instance.MakeRoomForRule(vss, rule, limit);
      }
      catch (Exception ex) when (ex is not OperationCanceledException)
      {
        Log.Warning(ex, "Could not make room for a new snapshot of rule {RuleName}", rule.Name);
      }
    }

    private static void RestartVss()
    {
      try
      {
        Log.Information("Restarting the Volume Shadow Copy service before the retry");
        VssServiceControl.Restart(TimeSpan.FromSeconds(60));
      }
      catch (Exception ex) when (ex is InvalidOperationException or System.ServiceProcess.TimeoutException or System.ComponentModel.Win32Exception)
      {
        Log.Warning(ex, "Could not restart the Volume Shadow Copy service");
      }
    }
  }
}
