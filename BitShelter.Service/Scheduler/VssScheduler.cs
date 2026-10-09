using BitShelter.Data;
using BitShelter.Models;
using BitShelter.Service.Jobs;
using Quartz;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BitShelter.Service.Scheduler
{
  public static class VssScheduler
  {
    public static IJobDetail SnapshotJob => JobBuilder.Create<SnapshotJob>()
          .WithIdentity("SnapshotJob")
          .Build();
    // Durable, so that it stays stored while no backup is queued.
    public static IJobDetail BackupJob => JobBuilder.Create<BackupJob>()
          .WithIdentity(Jobs.BackupJob.Key)
          .StoreDurably()
          .Build();
    public static IJobDetail PruneJob => JobBuilder.Create<PruneJob>()
          .WithIdentity("PruneJob")
          .Build();

    public static void CreateAllTriggers(IEnumerable<SnapshotRule> rules)
    {
      CreateAllTriggers(QuartzScheduler.Instance.Scheduler, rules);
    }

    // Replaces only the rule triggers. Pending snapshot retries and queued backups keep their triggers.
    internal static void CreateAllTriggers(IScheduler scheduler, IEnumerable<SnapshotRule> rules)
    {
      scheduler.UnscheduleJobs(GroupMatcher<TriggerKey>.GroupEquals(SnapshotRuleEx.TriggerGroup)).GetAwaiter().GetResult();

      if (!scheduler.Exists(Jobs.BackupJob.Key).GetAwaiter().GetResult())
        scheduler.AddJob(BackupJob).GetAwaiter().GetResult();

      CreatePruneJobTrigger(scheduler);

      foreach (SnapshotRule rule in rules.Where(r => r.Enabled))
        CreateSnapshotJobTrigger(scheduler, rule);
    }

    private static void CreatePruneJobTrigger(IScheduler scheduler)
    {
      ITrigger pruneTrigger = TriggerBuilder.Create()
        .WithIdentity("PruneJob")
        .WithSimpleSchedule(s => s.WithInterval(TimeSpan.FromMinutes(1)).RepeatForever())
        .Build();

      scheduler.ScheduleJob(PruneJob, new[] { pruneTrigger }, ScheduleJobOptions.Replacing).GetAwaiter().GetResult();
    }

    // A rule that cannot be scheduled (an invalid schedule, or no run between its start and end dates) must not
    // stop the other rules or the service.
    private static void CreateSnapshotJobTrigger(IScheduler scheduler, SnapshotRule rule)
    {
      try
      {
        if (rule.HasCalendar())
          scheduler.AddCalendar(rule.GetCalendarName(), rule.GetCalendar(), AddCalendarOptions.ReplacingAndUpdatingTriggers).GetAwaiter().GetResult();

        scheduler.ScheduleJob(SnapshotJob, new[] { rule.GetTrigger() }, ScheduleJobOptions.Replacing).GetAwaiter().GetResult();
      }
      catch (Exception ex) when (ex is SchedulerException or ArgumentException or FormatException)
      {
        Log.Warning(ex, "Rule {RuleName} is not scheduled: its schedule is invalid or has no next run", rule.Name);
      }
    }
  }
}
