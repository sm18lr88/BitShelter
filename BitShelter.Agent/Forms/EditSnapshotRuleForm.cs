using BitShelter.Models;
using System;
using System.Windows.Forms;
using System.Diagnostics;
using BitShelter.Data;
using Quartz;
using System.Threading.Tasks;
using System.Threading;

namespace BitShelter.Agent.Forms
{
  public partial class EditSnapshotRuleForm : Form
  {
    private bool Init { get; set; } = false;
    private bool Valid { get; set; } = true;
    private long RuleId { get; }
    private string RuleName { get; }
    private SynchronizationContext SyncContext { get; set; }



    //
    // Constructors

    protected EditSnapshotRuleForm(SnapshotRule rule)
    {
      InitializeComponent();
      SyncContext = SynchronizationContext.Current;

      InitBase();

      if (rule != null)
      {
        RuleId = rule.Id;
        RuleName = rule.Name;
        InitEdit(rule);
      }
      else
        RuleId = -1;

      Init = true;
      RefreshUI();
    }



    //
    // Initialization

    private void InitBase()
    {
      //
      // Set default control values
      tbName.Text = "New Schedule " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString();

      InitBaseGeneral();
      InitBaseSchedule();
      InitBaseBackup();
      InitBaseAdvanced();
    }

    private void InitEdit(SnapshotRule rule)
    {
      tbName.Text = rule.Name;
      cbEnabled.Checked = rule.Enabled;

      btnCreate.Text = "Save";

      InitEditSchedule(rule);
      InitEditGeneral(rule);
      InitEditBackup(rule);
      InitEditAdvanced(rule);
    }



    //
    // Form

    private static EditSnapshotRuleForm _instance = null;
    public static SnapshotRule DisplayInstance(SnapshotRule schedule = null)
    {
      _instance = _instance ?? new EditSnapshotRuleForm(schedule);

      _instance.ShowDialog();

      SnapshotRule ret = _instance.DialogResult == DialogResult.Yes
        ? _instance.SnapshotRule
        : null;

      _instance = null;

      return ret;
    }

    public static void CloseInstance()
    {
      if (_instance != null)
        _instance.Close();
    }


    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }

      _instance = null;

      base.Dispose(disposing);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
      if (keyData == Keys.Escape)
      {
        DialogResult = DialogResult.Cancel;

        Close();

        return true;
      }

      return base.ProcessCmdKey(ref msg, keyData);
    }



    //
    // Global controls

    private void btnCancel_Click(object sender, EventArgs e)
    {
      DialogResult = DialogResult.Cancel;

      Close();
    }

    private void btnCreate_Click(object sender, EventArgs e)
    {
      // This obviously should never happen.
      if (!Valid)
      {
        MessageBox.Show("Congratulation ! You have found a bug.\nThe schedule parameters are invalid, and the create button should be disabled.\n I would be very grateful if you could report on the GitHub page of the project with a screenshot of your schedule attached !\nTry cancelling and editing a schedule again to solve this issue. Sorry for this inconvenience.", "Error");
        return;
      }

      SnapshotRule = GenerateSchedule();

      DialogResult = DialogResult.Yes;

      Close();
    }

    private void LinkLabelClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
      Process.Start(new ProcessStartInfo(e.Link.LinkData as string) { UseShellExecute = true })?.Dispose();
    }

    private void refreshUI_Event(object sender, EventArgs e)
    {
      RefreshUI();
    }



    //
    // Misc

      private SnapshotRule GenerateSchedule()
    {
      var ret = new SnapshotRule()
      {
        Id = RuleId > 0 ? RuleId : DateTime.Now.Ticks,
        Name = tbName.Text,
        Enabled = cbEnabled.Checked
      };

      FillSchedule(ret);
      FillGeneral(ret);
      FillBackup(ret);
      FillAdvanced(ret);

      return ret;
    }

    private void RefreshUI()
    {
      if (!Init)
        return;

      ValidateSnapshot();
      ValidateGeneral();

      SnapshotRule rule = Valid ? GenerateSchedule() : null;

      btnCreate.Enabled = Valid;

      lblHumanSched.Text = Valid ? rule.ScheduleDescription : "Invalid schedule.";
      tbGenCron.Text = Valid ? rule.GeneratedCron : "";

      lblMaxCountVal.Text = Valid ? ComputeMaxSnapshotCount(rule) : "";

      if (Valid)
        SetStyleValid(lblHumanSched);

      else
        SetStyleInvalid(lblHumanSched);
    }

    private string ComputeMaxSnapshotCount(SnapshotRule rule)
    {
      var nextFireTime = rule.GetNextFireTime(DateTimeOffset.Now.AddSeconds(-1));

      if (!nextFireTime.HasValue)
        return "0";

      var lastFireTime = nextFireTime.Value.AddSeconds(rule.GetLifeTimeSeconds());

      CancellationTokenSource cancelSrc = new CancellationTokenSource();
      CancellationToken cancelToken = cancelSrc.Token;

      Task<int> computeTask =
        Task.Run(() => rule.GetFireCountBetween(nextFireTime.Value, lastFireTime, cancelToken));

      try
      {
        cancelSrc.CancelAfter(1000);
        computeTask.Wait(cancelToken);

        return computeTask.Result.ToString();
      }
      catch (OperationCanceledException)
      {
        return "(timeout: too many)";
      }
    }
  }
}
