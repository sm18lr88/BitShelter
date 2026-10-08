using BitShelter.Agent.Controls;
using BitShelter.Models;
using BitShelter.Models.Enums;
using BitShelter.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace BitShelter.Agent.Forms
{
  partial class EditSnapshotRuleForm
  {
    private List<BackupRule> backupRules = new List<BackupRule>();
    private Button btnBackupEdit;

    private void InitBaseBackup()
    {
      cbBackupTotalMaxSizeUnit.FillWithEnum(MediumStorageUnit.GB);

      var tip = new ToolTip(components);
      tip.SetToolTip(nbBackupTotalMaxSize, "Total size of all backups of this rule. 0 = no limit.");
      tip.SetToolTip(lblBackupTotalMaxSize, "Total size of all backups of this rule. 0 = no limit.");

      btnBackupEdit = new Button
      {
        Anchor = btnBackupAdd.Anchor,
        BackColor = btnBackupAdd.BackColor,
        ForeColor = btnBackupAdd.ForeColor,
        Location = new System.Drawing.Point(btnBackupDelete.Left - (btnBackupAdd.Left - btnBackupDelete.Left), btnBackupDelete.Top),
        Size = btnBackupDelete.Size,
        Text = "Edit",
      };
      tbBackup.Controls.Add(btnBackupEdit);

      glBackup.AutoGenerateColumns = false;
      glBackup.BackgroundColor = System.Drawing.SystemColors.Window;
      glBackup.ReadOnly = true;
      glBackup.AllowUserToAddRows = false;
      glBackup.AllowUserToDeleteRows = false;
      glBackup.MultiSelect = false;
      glBackup.RowHeadersVisible = false;
      glBackup.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
      glBackup.Columns.Add(BackupColumn(nameof(BackupRuleRow.Name), "Name", DataGridViewAutoSizeColumnMode.AllCells));
      glBackup.Columns.Add(BackupColumn(nameof(BackupRuleRow.Inputs), "Folders", DataGridViewAutoSizeColumnMode.Fill));
      glBackup.Columns.Add(BackupColumn(nameof(BackupRuleRow.When), "When", DataGridViewAutoSizeColumnMode.AllCells));
      glBackup.Columns.Add(BackupColumn(nameof(BackupRuleRow.Format), "Format", DataGridViewAutoSizeColumnMode.AllCells));

      btnBackupAdd.Click += (_, _) => AddBackupRule();
      btnBackupEdit.Click += (_, _) => EditSelectedBackupRule();
      btnBackupDelete.Click += (_, _) => DeleteSelectedBackupRule();
      glBackup.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) EditSelectedBackupRule(); };

      cbBackupEnable.Checked = false;
      RefreshBackupGrid();
      ChangeBackupEnabled(false);
    }

    private void InitEditBackup(SnapshotRule rule)
    {
      backupRules = (rule.BackupRules ?? new List<BackupRule>()).Select(Clone).ToList();
      cbBackupEnable.Checked = rule.BackupEnabled;
      nbBackupTotalMaxSize.Value = Math.Min(nbBackupTotalMaxSize.Maximum, Math.Max(nbBackupTotalMaxSize.Minimum, rule.BackupTotalMaxSize));
      cbBackupTotalMaxSizeUnit.SelectedIndex = cbBackupTotalMaxSizeUnit.Items.IndexOf(rule.BackupTotalMaxSizeUnit);
      cbBackupNotificationsEnabled.Checked = rule.BackupNotify;

      RefreshBackupGrid();
      ChangeBackupEnabled(rule.BackupEnabled);
    }

    private void cbBackupEnable_CheckedChanged(object sender, EventArgs e)
    {
      ChangeBackupEnabled(cbBackupEnable.Checked);
      RefreshUI();
    }

    private void ChangeBackupEnabled(bool enabled)
    {
      foreach (Control control in new Control[] { glBackup, cbBackupTotalMaxSizeUnit, nbBackupTotalMaxSize, cbBackupNotificationsEnabled, btnBackupAdd, btnBackupEdit, btnBackupDelete })
        control.Enabled = enabled;
    }

    private void AddBackupRule()
    {
      BackupRule backup = EditBackupRuleForm.Edit(this, null, backupRules.Select(b => b.Name));

      if (backup == null)
        return;

      backupRules.Add(backup);
      RefreshBackupGrid();
      RefreshUI();
    }

    private void EditSelectedBackupRule()
    {
      if (!(glBackup.CurrentRow?.DataBoundItem is BackupRuleRow row))
        return;

      BackupRule edited = EditBackupRuleForm.Edit(this, Clone(row.Rule), backupRules.Where(b => b != row.Rule).Select(b => b.Name));

      if (edited == null)
        return;

      backupRules[backupRules.IndexOf(row.Rule)] = edited;
      RefreshBackupGrid();
      RefreshUI();
    }

    private void DeleteSelectedBackupRule()
    {
      if (!(glBackup.CurrentRow?.DataBoundItem is BackupRuleRow row))
        return;

      backupRules.Remove(row.Rule);
      RefreshBackupGrid();
      RefreshUI();
    }

    private void RefreshBackupGrid()
    {
      glBackup.DataSource = new BindingList<BackupRuleRow>(backupRules.Select(b => new BackupRuleRow(b)).ToList());
    }

    private void FillBackup(SnapshotRule rule)
    {
      rule.BackupEnabled = cbBackupEnable.Checked;
      rule.BackupRules = backupRules.Select(Clone).ToList();
      rule.BackupTotalMaxSize = (int)nbBackupTotalMaxSize.Value;
      rule.BackupTotalMaxSizeUnit = (MediumStorageUnit)cbBackupTotalMaxSizeUnit.SelectedValue;
      rule.BackupNotify = cbBackupNotificationsEnabled.Checked;
    }

    // Every input folder must be on a drive that this rule snapshots, because backups read from the snapshot.
    private bool ValidateBackup()
    {
      if (!cbBackupEnable.Checked)
        return ValidateGeneric(() => true, cbBackupEnable);

      HashSet<string> volumes = cblDriveLetters.CheckedItems.Cast<Volume>().Select(v => v.DeviceID).ToHashSet(StringComparer.OrdinalIgnoreCase);

      return ValidateGeneric(() => backupRules.Count > 0 && backupRules.SelectMany(b => b.InputFolders).All(f => IsOnVolume(f, volumes)), cbBackupEnable);
    }

    private static bool IsOnVolume(string folder, HashSet<string> volumes)
    {
      try
      {
        return volumes.Contains(Volumes.GetUniqueVolumeNameForVolumeMountPoint(Volumes.GetVolumeRootPath(folder)));
      }
      catch (Win32Exception)
      {
        return false;
      }
    }

    private static BackupRule Clone(BackupRule rule)
    {
      return JsonConvert.DeserializeObject<BackupRule>(JsonConvert.SerializeObject(rule));
    }

    private static DataGridViewTextBoxColumn BackupColumn(string property, string header, DataGridViewAutoSizeColumnMode mode)
    {
      return new DataGridViewTextBoxColumn { DataPropertyName = property, HeaderText = header, AutoSizeMode = mode };
    }

    private sealed class BackupRuleRow
    {
      public BackupRuleRow(BackupRule rule)
      {
        Rule = rule;
      }

      public BackupRule Rule { get; }
      public string Name => Rule.Name;
      public string Inputs => string.Join(", ", Rule.InputFolders ?? new HashSet<string>()) + " -> " + Rule.OutputFolder;
      public string When => Math.Max(1, Rule.Every) == 1 ? "Every snapshot" : $"Every {Rule.Every} snapshots";
      public string Format => !Rule.CompressionEnabled ? "Folder copy"
        : $"{Rule.ArchiveType} {Rule.CompressionType}" + (Rule.Encryption == BackupEncryption.None ? "" : " + OpenPGP");
    }
  }
}
