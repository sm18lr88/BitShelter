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
    // Frequency

    private void rbFreqDaily_CheckedChanged(object sender, EventArgs e)
    {
      ChangeFreq(Freq.Daily);
    }

    private void rbFreqWeekly_CheckedChanged(object sender, EventArgs e)
    {
      ChangeFreq(Freq.Weekly);
    }

    private void rbFreqMonthly_CheckedChanged(object sender, EventArgs e)
    {
      ChangeFreq(Freq.Monthly);
    }

    private void rbFreqCron_CheckedChanged(object sender, EventArgs e)
    {
      ChangeFreq(Freq.Cron);
    }

    private void rbFreqWeeklyEvery_CheckedChanged(object sender, EventArgs e)
    {
      ChangeFreqWeekly(FreqWeekly.Weekly);
    }

    private void rbFreqWeeklyOn_CheckedChanged(object sender, EventArgs e)
    {
      ChangeFreqWeekly(FreqWeekly.OnDays);
    }

    private void cbFreqWeeklyDay_CheckedChanged(object sender, EventArgs e)
    {
      RefreshUI();
    }

    private void cblFreqMonthlyMonths_ItemCheckStateChanged(object sender, EWSoftware.ListControls.ItemCheckStateEventArgs e)
    {
      RefreshUI();
    }

    private void cblFreqMonthlyDays_ItemCheckStateChanged(object sender, EWSoftware.ListControls.ItemCheckStateEventArgs e)
    {
      RefreshUI();
    }

    private void tbFreqCron_TextChanged(object sender, EventArgs e)
    {
      RefreshUI();
    }

    private void cbFreqCronExcluding_CheckedChanged(object sender, EventArgs e)
    {
      ChangeCronExcludingEnabled(cbFreqCronExcluding.Checked);
    }

    private void ChangeCronExcludingEnabled(bool enabled)
    {
      dtpFreqCronExcludingFrom.Enabled = dtpFreqCronExcludingTo.Enabled = enabled;

      RefreshUI();
    }

    private void SelectFreqWeekly(FreqWeekly freq)
    {
      switch (freq)
      {
        case FreqWeekly.OnDays:
          rbFreqWeeklyOn.Checked = true;
          break;

        case FreqWeekly.Weekly:
          rbFreqWeeklyEvery.Checked = true;
          break;
      }
    }

      private void ChangeFreqWeekly(FreqWeekly freq)
    {
      nbFreqWeekly.Enabled = freq == FreqWeekly.Weekly;

      foreach (Control c in DayCheckboxes)
        c.Enabled = freq == FreqWeekly.OnDays;

      RefreshUI();
    }

    private void SelectFreq(Freq freq)
    {
      switch (freq)
      {
        case Freq.Daily:
          rbFreqDaily.Checked = true;
          break;

        case Freq.Weekly:
          rbFreqWeekly.Checked = true;
          break;

        case Freq.Monthly:
          rbFreqMonthly.Checked = true;
          break;

        case Freq.Cron:
          rbFreqCron.Checked = true;
          break;
      }
    }

    private void ChangeFreq(Freq freq)
    {
      bool daily = freq == Freq.Daily;
      bool weekly = freq == Freq.Weekly;
      bool monthly = freq == Freq.Monthly;
      bool cron = freq == Freq.Cron;

      gbDailyFreq.Enabled = !cron;

      plFreqDaily.Enabled = plFreqDaily.Visible = daily;
      plFreqWeekly.Enabled = plFreqWeekly.Visible = weekly;
      plFreqMonthly.Enabled = plFreqMonthly.Visible = monthly;
      plFreqCron.Enabled = plFreqCron.Visible = cron;

      RefreshUI();
    }
  }
}
