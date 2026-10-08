using BitShelter.Data;
using BitShelter.Models;
using BitShelter.Models.Enums;
using Quartz.Impl.Calendar;
using System;
using System.Threading;
using Xunit;

namespace BitShelter.Tests.Data
{
  public sealed class SnapshotRuleExTests
  {
    [Fact]
    public void ExcludingDayRange_from_daily_frequency_is_detected_and_returned()
    {
      var rule = new SnapshotRule
      {
        Id = 1,
        Name = "r",
        DailyFreq = DailyFreq.Every,
        DailyFreqEveryExcluding = true,
        DailyFreqEveryExcludingFrom = new DateTime(2000, 1, 1, 9, 0, 0),
        DailyFreqEveryExcludingTo = new DateTime(2000, 1, 1, 17, 0, 0),
      };

      Assert.True(rule.IsExcludingDayRange());
      Assert.True(rule.HasCalendar());

      rule.GetExcludingDayRange(out DateTime from, out DateTime to);
      Assert.Equal(rule.DailyFreqEveryExcludingFrom, from);
      Assert.Equal(rule.DailyFreqEveryExcludingTo, to);

      var cal = rule.GetCalendar();
      Assert.IsType<DailyCalendar>(cal);
    }

    [Fact]
    public void GetExcludingDayRange_without_exclusion_throws()
    {
      var rule = new SnapshotRule { Id = 2, Name = "r2" };
      Assert.False(rule.IsExcludingDayRange());
      Assert.Throws<InvalidOperationException>(() => rule.GetExcludingDayRange(out _, out _));
    }

    [Fact]
    public void GetLifeTimeSeconds_multiplies_value_by_unit_seconds()
    {
      var rule = new SnapshotRule
      {
        LifeTimeValue = 2,
        LifeTimeUnit = Timespan.Hour
      };

      Assert.Equal(7200, rule.GetLifeTimeSeconds());
    }

    [Fact]
    public void GetFireCountBetween_counts_occurrences_for_simple_cron()
    {
      var rule = new SnapshotRule
      {
        Id = 3,
        Name = "cron",
        GeneratedCron = "0 0/1 * * * ?",
        PeriodStart = new DateTime(2000, 1, 1)
      };

      var from = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
      var to = new DateTimeOffset(2025, 1, 1, 0, 5, 0, TimeSpan.Zero);

      int count = rule.GetFireCountBetween(from, to, CancellationToken.None);

      Assert.InRange(count, 4, 6);
    }
  }
}

