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
    // Properties

    public SnapshotRule SnapshotRule { get; private set; }

    private CheckBox[] DayCheckboxes { get; set; }



    //
    // Initialization

    private void InitBaseSchedule()
    {
      DayCheckboxes = new[] { cbFreqWeeklySunday, cbFreqWeeklyMonday, cbFreqWeeklyTuesday, cbFreqWeeklyWednesday, cbFreqWeeklyThursday, cbFreqWeeklyFriday, cbFreqWeeklySaturday };

      // Default: a snapshot every 4 hours. Missed times (computer off or asleep) are skipped, so a single
      // daily time can miss many days. 42 snapshots per week also fit the Windows default limit of 64.
      SelectDailyFreq(DailyFreq.Every);
      ChangeDailyFreq(DailyFreq.Every);
      nbDailyFreqEvery.Value = 4;
      ChangeFreq(Freq.Daily);
      ChangeFreqWeekly(FreqWeekly.Weekly);

      cbDailyFreqEveryExcluding.Checked = false;
      ChangeDailyFreqEveryExcludingEnabled(false);

      llbFreqCronHelp.Links.Add(0, 0, "https://www.quartz-scheduler.net/documentation/quartz-3.x/tutorial/crontriggers.html");
      llbFreqCronGen.Links.Add(0, 0, "https://www.freeformatter.com/cron-expression-generator-quartz.html");

      dtpPeriodStart.Value = DateTime.Today;
      dtpPeriodEnd.Value = DateTime.Today.AddMonths(1);

      cbDailyFreqEvery.SelectedIndex = 1;

      for (int i = 0; i < cblFreqMonthlyMonths.Items.Count; i++)
        cblFreqMonthlyMonths.SetItemChecked(i, true);

      cblFreqMonthlyDays.SetItemChecked(0, true);

      cbFreqCronExcluding.Checked = false;
      ChangeCronExcludingEnabled(false);
    }

    private void InitEditSchedule(SnapshotRule sched)
    {
      // Daily frequency
      SelectDailyFreq(sched.DailyFreq);
      dtpDailyFreqOnce.Value = sched.DailyFreqOnce;

      if (sched.DailyFreqEvery % 60 == 0)
      {
        nbDailyFreqEvery.Value = sched.DailyFreqEvery / 60;
        cbDailyFreqEvery.SelectedIndex = 1;
      }
      else
      {
        nbDailyFreqEvery.Value = sched.DailyFreqEvery;
        cbDailyFreqEvery.SelectedIndex = 0;
      }

      cbDailyFreqEveryExcluding.Checked = sched.DailyFreqEveryExcluding;
      dtpDailyFreqEveryExcludingFrom.Value = sched.DailyFreqEveryExcludingFrom;
      dtpDailyFreqEveryExcludingTo.Value = sched.DailyFreqEveryExcludingTo;


      // Frequency
      SelectFreq(sched.Freq);

      nbFreqDaily.Value = sched.FreqDailyEvery;

      SelectFreqWeekly(sched.FreqWeekly);
      nbFreqWeekly.Value = sched.FreqWeeklyEvery;

      for (int i = 0; i < DayCheckboxes.Length; i++)
        DayCheckboxes[i].Checked = sched.FreqWeeklyDays.Contains(i);

      for (int i = 0; i < cblFreqMonthlyMonths.Items.Count; i++)
        cblFreqMonthlyMonths.SetItemChecked(i, sched.FreqMonthlyMonths.Contains(i));

      for (int i = 0; i < cblFreqMonthlyDays.Items.Count; i++)
        cblFreqMonthlyDays.SetItemChecked(i, sched.FreqMonthlyDays.Contains(i));

      tbFreqCron.Text = sched.FreqCron;
      cbFreqCronExcluding.Checked = sched.FreqCronDailyExcluding;
      dtpFreqCronExcludingFrom.Value = sched.FreqCronDailyExcludingFrom;
      dtpFreqCronExcludingTo.Value = sched.FreqCronDailyExcludingTo;


      // Period
      dtpPeriodStart.Value = sched.PeriodStart;

      SelectPeriodEnd(sched.PeriodEndEnabled == false);
      dtpPeriodEnd.Value = sched.PeriodEnd;
    }



    //
    // Form


    //
    // General

    private void onChange_RefreshUI(object sender, EventArgs e)
    {
      RefreshUI();
    }


    //
    // Daily frequency

    private void rbDailyFreqOnce_CheckedChanged(object sender, EventArgs e)
    {
      ChangeDailyFreq(DailyFreq.Once);
    }

    private void rbDailyFreqEvery_CheckedChanged(object sender, EventArgs e)
    {
      ChangeDailyFreq(DailyFreq.Every);
    }

    private void cbDailyFreqEveryExcluding_CheckedChanged(object sender, EventArgs e)
    {
      ChangeDailyFreqEveryExcludingEnabled(cbDailyFreqEveryExcluding.Checked);
    }

    private void SelectDailyFreq(DailyFreq freq)
    {
      switch (freq)
      {
        case DailyFreq.Once:
          rbDailyFreqOnce.Checked = true;
          break;

        case DailyFreq.Every:
          rbDailyFreqEvery.Checked = true;
          break;
      }
    }

    private void ChangeDailyFreq(DailyFreq freq)
    {
      bool once = freq == DailyFreq.Once;

      dtpDailyFreqOnce.Enabled = once;

      nbDailyFreqEvery.Enabled = !once;
      cbDailyFreqEvery.Enabled = !once;
      cbDailyFreqEveryExcluding.Enabled = !once;

      RefreshUI();
    }

    private void ChangeDailyFreqEveryExcludingEnabled(bool enabled)
    {
      dtpDailyFreqEveryExcludingFrom.Enabled = dtpDailyFreqEveryExcludingTo.Enabled = enabled;
      
      RefreshUI();
    }



    //
    // Period

    private void rbPeriodEndNever_CheckedChanged(object sender, EventArgs e)
    {
      ChangePeriodEnd(true);
    }

    private void rbPeriodEndDate_CheckedChanged(object sender, EventArgs e)
    {
      ChangePeriodEnd(false);
    }

    private void SelectPeriodEnd(bool never)
    {
      rbPeriodEndDate.Checked = !never;
    }

      private void ChangePeriodEnd(bool never)
    {
      dtpPeriodEnd.Enabled = !never;

      RefreshUI();
    }
  }
}
