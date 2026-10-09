using BitShelter.Agent;
using BitShelter.Agent.Forms;
using BitShelter.Agent.Ipc;
using BitShelter.Models;
using Serilog;
using System;
using System.Windows.Forms;

namespace BitShelter
{
  public partial class SettingsForm : Form
  {
    protected static SettingsForm _instance = null;



    public static SettingsForm DisplayInstance()
    {
      _instance = _instance ?? new SettingsForm();

      _instance.Show();
      _instance.Focus();
      return _instance;
    }

    public static void CloseInstance()
    {
      _instance?.Close();
    }


    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposing)
        components?.Dispose();

      CleanupSnapshotClient();
      _instance = null;

      base.Dispose(disposing);
    }

    protected SettingsForm()
    {
      InitializeComponent();
      Icon = BitShelter.Agent.AppIcon.Load();

      SetupSnapshotDataGrid();
      ConnectSnapshotClient();
    }

    private void SetupSnapshotDataGrid()
    {
      dgSnapshotRules.AutoGenerateColumns = false;
      dgSnapshotRules.BackgroundColor = System.Drawing.SystemColors.Window;
      dgSnapshotRules.CellContentClick += DgSnapshotRules_CellContentClick;

      SetupSnapshotDataGridColumns();
    }

    private void SetupSnapshotDataGridColumns()
    {
      dgSnapshotRules.Columns.Add(CreateCheckboxColumn("Enabled", "On", DataGridViewAutoSizeColumnMode.AllCells));
      dgSnapshotRules.Columns.Add(CreateTextColumn("Name", "Name", DataGridViewAutoSizeColumnMode.AllCells));
      dgSnapshotRules.Columns.Add(CreateTextColumn("VolumesAsString", "Volumes", DataGridViewAutoSizeColumnMode.AllCells));
      dgSnapshotRules.Columns.Add(CreateTextColumn("ScheduleDescription", "Schedule", DataGridViewAutoSizeColumnMode.Fill));
      dgSnapshotRules.Columns.Add(CreateButtonColumn("Edit", "Edit", DataGridViewAutoSizeColumnMode.AllCells));
      dgSnapshotRules.Columns.Add(CreateButtonColumn("Delete", "Delete", DataGridViewAutoSizeColumnMode.AllCells));
    }

    private bool RefreshDataGrid()
    {
      //snap.InnerDuplexChannel.Faulted
      try
      {
        dgSnapshotRules.DataSource = SnapshotClient.GetRules();

        return true;
      }
      catch (Exception ex)
      {
        Log.Error(ex, "Error while refreshing Agent SnapshotRules Grid.");

        return false;
      }
    }

    private bool AddOrUpdateSnapshotRule(SnapshotRule rule)
    {
      try
      {
        bool saved = SnapshotClient.AddOrUpdateRule(rule);

        dgSnapshotRules.DataSource = SnapshotClient.GetRules();

        if (!saved)
          ShowError("The service could not save the rule. See the service log for details.");

        return saved;
      }
      catch (Exception ex)
      {
        Log.Error(ex, "Error while requesting Addition/Edition of SnapshotRule {Id}.", rule?.Id);
        ShowError("The rule was not saved: " + ex.Message);

        return false;
      }
    }

    private bool DeleteSnapshotRule(SnapshotRule rule, bool deleteSnapshots)
    {
      try
      {
        bool deleted = SnapshotClient.DeleteRule(rule, deleteSnapshots);

        dgSnapshotRules.DataSource = SnapshotClient.GetRules();

        if (!deleted)
          ShowError("The service could not delete the rule. See the service log for details.");

        return deleted;
      }
      catch (Exception ex)
      {
        Log.Error(ex, "Error while requesting Deletion of SnapshotRule {Id}.", rule?.Id);
        ShowError("The rule was not deleted: " + ex.Message);

        return false;
      }
    }

    private void ShowError(string message)
    {
      MessageBox.Show(this, message, "BitShelter", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private DataGridViewButtonColumn CreateButtonColumn(string mappingName, string headerName, DataGridViewAutoSizeColumnMode autoSizeMode, int? width = null)
    {
      var ret = CreateColumnGeneric<DataGridViewButtonColumn>(mappingName, headerName, autoSizeMode, width);

      ret.Text = headerName;
      ret.UseColumnTextForButtonValue = true;

      return ret;
    }

    private DataGridViewCheckBoxColumn CreateCheckboxColumn(string mappingName, string headerName, DataGridViewAutoSizeColumnMode autoSizeMode, int? width = null)
    {
      return CreateColumnGeneric<DataGridViewCheckBoxColumn>(mappingName, headerName, autoSizeMode, width);
    }

    private DataGridViewTextBoxColumn CreateTextColumn(string mappingName, string headerName, DataGridViewAutoSizeColumnMode autoSizeMode, int? width = null)
    {
      return CreateColumnGeneric<DataGridViewTextBoxColumn>(mappingName, headerName, autoSizeMode, width);
    }

    private T CreateColumnGeneric<T>(string mappingName, string headerName, DataGridViewAutoSizeColumnMode autoSizeMode, int? width)
      where T : DataGridViewColumn, new()
    {
      T ret = new T();

      ret.Name = mappingName;
      ret.DataPropertyName = mappingName;
      ret.HeaderText = headerName;
      ret.AutoSizeMode = autoSizeMode;

      if (width != null)
        ret.Width = width.Value;

      return ret;
    }

    private void DgSnapshotRules_CellContentClick(object sender, DataGridViewCellEventArgs e)
    {
      var senderGrid = (DataGridView)sender;

      if (senderGrid.Columns[e.ColumnIndex] is DataGridViewButtonColumn &&
          e.RowIndex >= 0)
      {
        SnapshotRule rule = senderGrid.Rows[e.RowIndex].DataBoundItem as SnapshotRule;

        switch (senderGrid.Columns[e.ColumnIndex].Name)
        {
          case "Edit":
            rule = EditSnapshotRuleForm.DisplayInstance(rule);

            if (rule != null)
              AddOrUpdateSnapshotRule(rule);
            break;

          case "Delete":
            var res = MessageBox.Show("Do you also want to delete existing Snapshots ?", "Confirm", MessageBoxButtons.YesNoCancel);

            if (res != DialogResult.Cancel)
              DeleteSnapshotRule(rule, res == DialogResult.Yes);

            break;
        }
      }
    }

    private void btnAddSchedule_Click(object sender, EventArgs e)
    {
      SnapshotRule rule = EditSnapshotRuleForm.DisplayInstance();

      if (rule != null)
        AddOrUpdateSnapshotRule(rule);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
      if (keyData == Keys.Escape)
      {
        Close();
        return true;
      }

      return base.ProcessCmdKey(ref msg, keyData);
    }
  }
}
