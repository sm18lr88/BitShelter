using BitShelter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using BitShelter.Agent.Helpers;
using System.Windows.Forms;

namespace BitShelter.Agent.Forms
{
  partial class EditSnapshotRuleForm
  {
    //
    // Misc

    private void FillSchedule(SnapshotRule sched)
    {
      // Daily Freq
      sched.DailyFreq = GetDailyFreq();
      sched.DailyFreqOnce = dtpDailyFreqOnce.Value;
      sched.DailyFreqEvery = (int)nbDailyFreqEvery.Value * (cbDailyFreqEvery.SelectedIndex == 0 ? 1 : 60);
      sched.DailyFreqEveryExcluding = cbDailyFreqEveryExcluding.Checked;
      sched.DailyFreqEveryExcludingFrom = dtpDailyFreqEveryExcludingFrom.Value;
      sched.DailyFreqEveryExcludingTo = dtpDailyFreqEveryExcludingTo.Value;
        
      // Frequency
      sched.Freq = GetFreq();
      sched.FreqDailyEvery = (int)nbFreqDaily.Value;
      sched.FreqWeekly = GetFreqWeekly();
      sched.FreqWeeklyEvery = (int)nbFreqWeekly.Value;
      sched.FreqWeeklyDays = GetWeeklyDays();
      sched.FreqMonthlyMonths = GetMonthlyMonths();
      sched.FreqMonthlyDays = GetMonthlyDays();
      sched.FreqCron = tbFreqCron.Text;
      sched.FreqCronDailyExcluding = cbFreqCronExcluding.Checked;
      sched.FreqCronDailyExcludingFrom = dtpFreqCronExcludingFrom.Value;
      sched.FreqCronDailyExcludingTo = dtpFreqCronExcludingTo.Value;

      // Period
      sched.PeriodStart = dtpPeriodStart.Value;
      sched.PeriodEndEnabled = rbPeriodEndDate.Checked;
      sched.PeriodEnd = dtpPeriodEnd.Value;

      sched.GeneratedCron = ScheduleUtils.GenerateCron(sched);
      sched.ScheduleDescription = ScheduleUtils.GetHumanizedSchedule(sched);
    }



    //
    // Getter

    private HashSet<int> GetWeeklyDays()
    {
      return new HashSet<int>(
        DayCheckboxes.Select((cb, idx) => cb.Checked ? idx : -1)
                     .Where(idx => idx >= 0)
      );
    }

    private HashSet<int> GetMonthlyMonths()
    {
      return new HashSet<int>(
        cblFreqMonthlyMonths.CheckedIndices
      );
    }

    private HashSet<int> GetMonthlyDays()
    {
      return new HashSet<int>(
        cblFreqMonthlyDays.CheckedIndices
      );
    }

    private DailyFreq GetDailyFreq()
    {
      if (rbDailyFreqOnce.Checked)
        return DailyFreq.Once;

      else if (rbDailyFreqEvery.Checked)
        return DailyFreq.Every;

      else
        return DailyFreq.Unknown;
    }

    private Freq GetFreq()
    {
      if (rbFreqDaily.Checked)
        return Freq.Daily;

      else if (rbFreqWeekly.Checked)
        return Freq.Weekly;

      else if (rbFreqMonthly.Checked)
        return Freq.Monthly;

      else if (rbFreqCron.Checked)
        return Freq.Cron;

      else
        return Freq.Unknown;
    }

    private FreqWeekly GetFreqWeekly()
    {
      if (rbFreqWeeklyEvery.Checked)
        return FreqWeekly.Weekly;

      else if (rbFreqWeeklyOn.Checked)
        return FreqWeekly.OnDays;

      else
        return FreqWeekly.Unknown;
    }



    //
    // Validation

    private bool ValidateSchedule()
    {
      bool valid = true;

      valid = ValidateDailyTimeRange() && valid;
      valid = ValidateFreqWeekly() && valid;
      valid = ValidateFreqMonthly() && valid;
      valid = ValidateFreqCron() && valid;
      valid = ValidatePeriod() && valid;
      valid = GetFreq() != Freq.Unknown && valid;
      valid = GetDailyFreq() != DailyFreq.Unknown && valid;

      return valid;
    }

    private bool ValidateDailyTimeRange()
    {
      if (rbDailyFreqOnce.Checked)
        return true;

      if (dtpDailyFreqEveryExcludingTo.Value.TimeOfDay <= dtpDailyFreqEveryExcludingFrom.Value.TimeOfDay)
      {
        // Display error
        SetStyleInvalid(lblDailyFreqEveryStartAt, lblDailyFreqEveryEndAt);

        return false;
      }

      else
      {
        // Restore font if necessary
        SetStyleValid(lblDailyFreqEveryStartAt, lblDailyFreqEveryEndAt);

        return true;
      }
    }

    private bool ValidateFreqWeekly()
    {
      if (rbFreqWeekly.Checked == false || rbFreqWeeklyOn.Checked == false)
        return true;

      bool valid = DayCheckboxes.Any(cb => cb.Checked);

      if (!valid)
        SetStyleInvalid(DayCheckboxes);

      else
        SetStyleValid(DayCheckboxes);

      return valid;
    }

    private bool ValidateFreqMonthly()
    {
      if (rbFreqMonthly.Checked == false)
        return true;

      bool valid = true;

      if (cblFreqMonthlyMonths.CheckedIndices.Count == 0)
      {
        // Display error
        SetStyleInvalid(lblFreqMonthlyMonths);

        valid = false;
      }
      else
        // Restore font if necessary
        SetStyleValid(lblFreqMonthlyMonths);

      if (cblFreqMonthlyDays.CheckedIndices.Count == 0)
      {
        // Display error
        SetStyleInvalid(lblFreqMonthlyDays);

        valid = false;
      }
      else
        // Restore font if necessary
        SetStyleValid(lblFreqMonthlyDays);

      return valid;
    }

    private bool ValidateFreqCron()
    {
      if (rbFreqCron.Checked == false)
        return true;

      bool valid = true;

      if (!Quartz.CronExpression.TryParse(tbFreqCron.Text, out _))
      {
        // Display error
        SetStyleInvalid(lblFreqCron);

        valid = false;
      }
      else
        // Restore font if necessary
        SetStyleValid(lblFreqCron);

      if (!cbFreqCronExcluding.Checked && dtpFreqCronExcludingTo.Value <= dtpFreqCronExcludingFrom.Value)
      {
        // Display error
        SetStyleInvalid(lblFreqCronStart, lblFreqCronEnd);

        valid = false;
      }
      else
        // Restore font if necessary
        SetStyleValid(lblFreqCronStart, lblFreqCronEnd);

      return valid;
    }

    private bool ValidatePeriod()
    {
      if (!rbPeriodEndDate.Checked && dtpPeriodEnd.Value <= dtpPeriodStart.Value)
      {
        // Display error
        SetStyleInvalid(lblPeriodStart, lblPeriodEnd);

        return false;
      }

      else
      {
        // Restore font if necessary
        SetStyleValid(lblPeriodStart, lblPeriodEnd);

        return true;
      }
    }
  }
}
