using BitShelter.Data;
using BitShelter.Models;
using Quartz;
using System.Collections.Concurrent;

namespace BitShelter.Tests.Data
{
  public sealed class SnapshotTriggerSchedulingTests
  {
    public sealed class ProbeJob : IJob
    {
      public static readonly ConcurrentQueue<long> Fired = new();

      public long RuleId { get; set; }

      public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
      {
        Fired.Enqueue(RuleId);
        return ValueTask.CompletedTask;
      }
    }

    [Fact]
    public async Task Rule_trigger_rebuilt_with_past_start_waits_for_its_next_real_fire_time()
    {
      const long RuleId = 1001;
      const long DefaultPolicyRuleId = 2002;
      const long StartNowRuleId = 3003;

      string farFromNowCron = $"0 0 {(DateTime.Now.Hour + 12) % 24} * * ?";
      DateTime periodStart = DateTime.Now.AddDays(-30);

      var rule = new SnapshotRule { Id = RuleId, Name = "far", GeneratedCron = farFromNowCron, PeriodStart = periodStart };

      ITrigger defaultPolicyTrigger = TriggerBuilder.Create()
        .WithIdentity("default-policy", "SnapshotJob")
        .WithCronSchedule(farFromNowCron)
        .StartAt(periodStart)
        .UsingJobData("RuleId", DefaultPolicyRuleId)
        .Build();

      ITrigger startNowTrigger = TriggerBuilder.Create()
        .WithIdentity("start-now", "SnapshotJob")
        .StartNow()
        .UsingJobData("RuleId", StartNowRuleId)
        .Build();

      IScheduler scheduler = await QuartzSchedulerBuilder
        .Create(q => q.ConfigureScheduler(o => o.InstanceName = $"tests-{Guid.NewGuid():N}").UseInMemoryStore())
        .BuildScheduler(TestContext.Current.CancellationToken);

      try
      {
        IJobDetail job = JobBuilder.Create<ProbeJob>().WithIdentity("probe").Build();
        await scheduler.ScheduleJob(job, new[] { rule.GetTrigger(), defaultPolicyTrigger, startNowTrigger }, ScheduleJobOptions.Replacing, TestContext.Current.CancellationToken);
        await scheduler.Start(TestContext.Current.CancellationToken);

        // The start-now trigger proves jobs execute and receive RuleId by property injection (SnapshotJob relies on both).
        DateTime deadline = DateTime.UtcNow.AddSeconds(10);
        while (!ProbeJob.Fired.Contains(StartNowRuleId) && DateTime.UtcNow < deadline)
          await Task.Delay(50, TestContext.Current.CancellationToken);

        await Task.Delay(1500, TestContext.Current.CancellationToken);

        Assert.Contains(StartNowRuleId, ProbeJob.Fired);
        Assert.DoesNotContain(RuleId, ProbeJob.Fired);
        Assert.DoesNotContain(DefaultPolicyRuleId, ProbeJob.Fired);
      }
      finally
      {
        await scheduler.Shutdown(waitForJobsToComplete: false, TestContext.Current.CancellationToken);
      }
    }
  }
}
