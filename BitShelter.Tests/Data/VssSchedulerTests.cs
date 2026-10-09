using BitShelter.Data;
using BitShelter.Models;
using BitShelter.Service.Jobs;
using BitShelter.Service.Scheduler;
using Quartz;

namespace BitShelter.Tests.Data
{
  public sealed class VssSchedulerTests
  {
    private const string DailyAt8 = "0 0 8 * * ?";

    [Fact]
    public async Task Rebuilding_rule_triggers_skips_a_bad_rule_and_keeps_retries_and_the_backup_job()
    {
      CancellationToken ct = TestContext.Current.CancellationToken;
      var good = new SnapshotRule { Id = 1, Name = "good", Enabled = true, GeneratedCron = DailyAt8, PeriodStart = DateTime.Now.AddDays(-1) };
      var endsBeforeStart = new SnapshotRule
      {
        Id = 2, Name = "ends before start", Enabled = true, GeneratedCron = DailyAt8,
        PeriodStart = DateTime.Now, PeriodEndEnabled = true, PeriodEnd = DateTime.Now.AddDays(-1),
      };

      IScheduler scheduler = await QuartzSchedulerBuilder
        .Create(q => q.ConfigureScheduler(o => o.InstanceName = $"tests-{Guid.NewGuid():N}").UseInMemoryStore())
        .BuildScheduler(ct);

      try
      {
        VssScheduler.CreateAllTriggers(scheduler, new[] { endsBeforeStart, good });

        ITrigger retry = TriggerBuilder.Create()
          .WithIdentity("1 Retry 0", "SnapshotJob")
          .ForJob(VssScheduler.SnapshotJob)
          .StartAt(DateTimeOffset.Now.AddMinutes(1))
          .Build();
        await scheduler.ScheduleJob(retry, cancellationToken: ct);

        Assert.True(await scheduler.Exists(good.GetTrigger().Key, ct));
        Assert.False(await scheduler.Exists(new TriggerKey(endsBeforeStart.ToString(), SnapshotRuleEx.TriggerGroup), ct));

        VssScheduler.CreateAllTriggers(scheduler, Array.Empty<SnapshotRule>());

        Assert.False(await scheduler.Exists(good.GetTrigger().Key, ct));
        Assert.True(await scheduler.Exists(retry.Key, ct));
        Assert.True(await scheduler.Exists(BackupJob.Key, ct));
      }
      finally
      {
        await scheduler.Shutdown(waitForJobsToComplete: false, ct);
      }
    }
  }
}
