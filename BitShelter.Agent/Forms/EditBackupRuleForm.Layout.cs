using SharpCompress.Common;
using System.Drawing;
using System.Windows.Forms;

namespace BitShelter.Agent.Forms
{
  // Builds the controls of the backup editor in code (no designer layout).
  partial class EditBackupRuleForm
  {
    private readonly TextBox tbName = new TextBox { Dock = DockStyle.Fill };
    private readonly ListBox lbInputs = new ListBox { Dock = DockStyle.Fill, Height = 80, IntegralHeight = false };
    private readonly Button btnInputAdd = new Button { Text = "Add...", AutoSize = true };
    private readonly Button btnInputRemove = new Button { Text = "Remove", AutoSize = true };
    private readonly TextBox tbOutput = new TextBox { Width = 330 };
    private readonly Button btnOutputBrowse = new Button { Text = "Browse...", AutoSize = true };
    private readonly TextBox tbIncludes = Multiline(48);
    private readonly TextBox tbExcludes = Multiline(48);
    private readonly NumericUpDown nbEvery = Number(1, 100000, 1);
    private readonly NumericUpDown nbOffset = Number(0, 100000, 0);
    private readonly NumericUpDown nbMaxSize = Number(0, 100000000, 0);
    private readonly NumericUpDown nbMaxCount = Number(0, 100000, 10);
    private readonly CheckBox cbArchive = new CheckBox { Text = "Create an archive", AutoSize = true, Checked = true };
    private readonly ComboBox cbArchiveType = Choice();
    private readonly ComboBox cbCompression = Choice();
    private readonly ComboBox cbEncryption = Choice();
    private readonly ComboBox cbCipher = Choice();
    private readonly TextBox tbPublicKey = Multiline(64);
    private readonly Button btnPublicKeyLoad = new Button { Text = "Load from file...", AutoSize = true };
    private readonly Label lblPublicKeyInfo = new Label { AutoSize = true, ForeColor = Color.DimGray };
    private readonly TextBox tbPassphrase = new TextBox { Width = 200, UseSystemPasswordChar = true };
    private readonly TextBox tbPassphraseConfirm = new TextBox { Width = 200, UseSystemPasswordChar = true };
    private readonly CheckBox cbNotifyError = new CheckBox { Text = "When a backup fails", AutoSize = true, Checked = true };
    private readonly CheckBox cbNotifyComplete = new CheckBox { Text = "When a backup completes", AutoSize = true };
    private readonly Button btnOk = new Button { Text = "OK", AutoSize = true, MinimumSize = new Size(80, 0) };
    private readonly Button btnCancel = new Button { Text = "Cancel", AutoSize = true, MinimumSize = new Size(80, 0), DialogResult = DialogResult.Cancel };
    private Control publicKeyRow, publicKeyLabel, passphraseRow, passphraseLabel;

    private void BuildLayout()
    {
      Text = "Backup";
      // Sizes below are in 96-DPI pixels. Windows scales them to the monitor DPI when the form opens.
      AutoScaleDimensions = new SizeF(96F, 96F);
      AutoScaleMode = AutoScaleMode.Dpi;
      FormBorderStyle = FormBorderStyle.FixedDialog;
      MaximizeBox = MinimizeBox = false;
      StartPosition = FormStartPosition.CenterParent;
      AutoSize = true;
      AutoSizeMode = AutoSizeMode.GrowAndShrink;
      AcceptButton = btnOk;
      CancelButton = btnCancel;

      var table = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Padding = new Padding(10) };
      table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
      table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 560));

      AddRow(table, "Name:", tbName);
      AddRow(table, "Folders to back up:", Stack(lbInputs, Flow(btnInputAdd, btnInputRemove)));
      AddRow(table, "Output folder:", Flow(tbOutput, btnOutputBrowse));
      AddRow(table, "Include patterns:", Stack(tbIncludes, Hint("One pattern per line. Empty: all files. Glob by default, \"regex:\" for a regular expression.")));
      AddRow(table, "Exclude patterns:", tbExcludes);
      AddRow(table, "When:", Flow(Caption("After every"), nbEvery, Caption("snapshots, starting with snapshot number"), nbOffset));
      AddRow(table, "Limits:", Flow(Caption("Max. size per backup (MB, 0 = no limit):"), nbMaxSize, Caption("Keep:"), nbMaxCount, Caption("backups (0 = all)")));
      AddRow(table, "Format:", Flow(cbArchive, cbArchiveType, cbCompression));
      AddRow(table, "Encryption:", Flow(cbEncryption, cbCipher));
      publicKeyRow = Stack(tbPublicKey, Flow(btnPublicKeyLoad, lblPublicKeyInfo));
      publicKeyLabel = AddRow(table, "OpenPGP public key:", publicKeyRow);
      passphraseRow = Flow(tbPassphrase, Caption("Confirm:"), tbPassphraseConfirm);
      passphraseLabel = AddRow(table, "Passphrase:", passphraseRow);
      AddRow(table, "Notify:", Flow(cbNotifyError, cbNotifyComplete, Hint("(needs \"Enable desktop notifications\" on the Backup tab)")));
      AddRow(table, "", Flow(btnOk, btnCancel));

      Controls.Add(table);

      cbArchiveType.DataSource = new[] { ArchiveType.Zip, ArchiveType.Tar };
    }

    private static Label AddRow(TableLayoutPanel table, string label, Control control)
    {
      var labelControl = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Top, Margin = new Padding(3, 6, 3, 3) };

      table.Controls.Add(labelControl);
      table.Controls.Add(control);
      control.Dock = DockStyle.Fill;

      return labelControl;
    }

    private static TableLayoutPanel Stack(params Control[] controls)
    {
      var panel = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, Margin = Padding.Empty };

      foreach (Control control in controls)
      {
        control.Dock = DockStyle.Fill;
        panel.Controls.Add(control);
      }

      return panel;
    }

    private static FlowLayoutPanel Flow(params Control[] controls)
    {
      var panel = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Margin = Padding.Empty };
      panel.Controls.AddRange(controls);

      return panel;
    }

    private static Label Caption(string text) => new Label { Text = text, AutoSize = true, Margin = new Padding(3, 6, 3, 3) };
    private static Label Hint(string text) => new Label { Text = text, AutoSize = true, ForeColor = Color.DimGray, MaximumSize = new Size(550, 0) };
    private static TextBox Multiline(int height) => new TextBox { Multiline = true, Height = height, ScrollBars = ScrollBars.Vertical, AcceptsReturn = true };
    private static ComboBox Choice(int width = 170) => new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = width, FormattingEnabled = true };
    private static NumericUpDown Number(int min, int max, int value) => new NumericUpDown { Minimum = min, Maximum = max, Value = value, Width = 80 };
  }
}
