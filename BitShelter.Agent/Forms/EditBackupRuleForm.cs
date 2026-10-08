using BitShelter.Backup;
using BitShelter.Encryption;
using BitShelter.IO;
using BitShelter.Models;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace BitShelter.Agent.Forms
{
  // Creates or edits one backup rule of a snapshot rule.
  public partial class EditBackupRuleForm : Form
  {
    private readonly BackupRule original;
    private readonly HashSet<string> otherNames;

    private EditBackupRuleForm(BackupRule rule, IEnumerable<string> otherNames)
    {
      InitializeComponent();
      BuildLayout();

      original = rule;
      this.otherNames = new HashSet<string>(otherNames, StringComparer.OrdinalIgnoreCase);

      cbEncryption.DataSource = Enum.GetValues(typeof(BackupEncryption));
      cbCipher.DataSource = OpenPgpEncryption.Ciphers.ToList();
      cbCipher.Format += (_, e) => e.Value = ((EncryptionAlgorithm)e.ListItem).GetDisplayName();
      cbEncryption.Format += (_, e) => e.Value = DescribeEncryption((BackupEncryption)e.ListItem);

      btnInputAdd.Click += (_, _) => AddInputFolder();
      btnInputRemove.Click += (_, _) => { if (lbInputs.SelectedItem != null) lbInputs.Items.Remove(lbInputs.SelectedItem); };
      btnOutputBrowse.Click += (_, _) => { string folder = PickFolder(tbOutput.Text); if (folder != null) tbOutput.Text = folder; };
      btnPublicKeyLoad.Click += (_, _) => LoadPublicKey();
      tbPublicKey.TextChanged += (_, _) => DescribePublicKey();
      cbArchive.CheckedChanged += (_, _) => RefreshState();
      cbArchiveType.SelectedIndexChanged += (_, _) => FillCompressions();
      cbEncryption.SelectedIndexChanged += (_, _) => RefreshState();
      btnOk.Click += (_, _) => Save();

      cbArchiveType.SelectedItem = ArchiveType.Zip;
      FillCompressions();
      cbCipher.SelectedItem = OpenPgpEncryption.DefaultAlgorithm;

      if (rule != null)
        LoadRule(rule);

      RefreshState();
    }

    public BackupRule Result { get; private set; }

    // Returns the new or edited backup rule, or null when the user cancels.
    public static BackupRule Edit(IWin32Window owner, BackupRule rule, IEnumerable<string> otherNames)
    {
      using (var form = new EditBackupRuleForm(rule, otherNames))
        return form.ShowDialog(owner) == DialogResult.OK ? form.Result : null;
    }

    private void LoadRule(BackupRule rule)
    {
      tbName.Text = rule.Name;
      lbInputs.Items.AddRange((rule.InputFolders ?? new HashSet<string>()).Cast<object>().ToArray());
      tbOutput.Text = rule.OutputFolder;
      tbIncludes.Lines = FormatPatterns(rule.FilterIncludes);
      tbExcludes.Lines = FormatPatterns(rule.FilterExcludes);
      nbEvery.Value = Math.Max(1, Math.Min(rule.Every, (int)nbEvery.Maximum));
      nbOffset.Value = Math.Max(0, Math.Min(rule.Offset, (int)nbOffset.Maximum));
      nbMaxSize.Value = Math.Max(0, Math.Min(rule.MaxSizeMB, (int)nbMaxSize.Maximum));
      nbMaxCount.Value = Math.Max(0, Math.Min(rule.MaxBackupCount, (int)nbMaxCount.Maximum));
      cbArchive.Checked = rule.CompressionEnabled;
      cbArchiveType.SelectedItem = rule.ArchiveType == ArchiveType.Tar ? ArchiveType.Tar : ArchiveType.Zip;
      FillCompressions();
      cbCompression.SelectedItem = rule.CompressionType;
      cbEncryption.SelectedItem = rule.Encryption;
      cbCipher.SelectedItem = OpenPgpEncryption.IsSupported(rule.EncryptionType) ? rule.EncryptionType : OpenPgpEncryption.DefaultAlgorithm;
      tbPublicKey.Text = rule.PgpPublicKey;
      tbPassphrase.PlaceholderText = tbPassphraseConfirm.PlaceholderText = rule.ProtectedPassphrase != null ? "(unchanged)" : "";
      cbNotifyError.Checked = rule.NotifyError;
      cbNotifyComplete.Checked = rule.NotifyComplete;
    }

    private void Save()
    {
      try
      {
        Result = BuildRule();
        DialogResult = DialogResult.OK;
        Close();
      }
      catch (ArgumentException ex)
      {
        MessageBox.Show(this, ex.Message, "Backup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
      }
    }

    private BackupRule BuildRule()
    {
      var encryption = (BackupEncryption)cbEncryption.SelectedItem;
      var rule = new BackupRule
      {
        Name = tbName.Text.Trim(),
        InputFolders = new HashSet<string>(lbInputs.Items.Cast<string>(), StringComparer.OrdinalIgnoreCase),
        OutputFolder = tbOutput.Text.Trim(),
        FilterIncludes = ParsePatterns(tbIncludes.Lines),
        FilterExcludes = ParsePatterns(tbExcludes.Lines),
        Every = (int)nbEvery.Value,
        Offset = (int)nbOffset.Value,
        MaxSizeMB = (int)nbMaxSize.Value,
        MaxBackupCount = (int)nbMaxCount.Value,
        CompressionEnabled = cbArchive.Checked,
        ArchiveType = (ArchiveType)cbArchiveType.SelectedItem,
        CompressionType = (CompressionType)cbCompression.SelectedItem,
        Encryption = encryption,
        EncryptionType = encryption == BackupEncryption.None ? EncryptionAlgorithm.None : (EncryptionAlgorithm)cbCipher.SelectedItem,
        PgpPublicKey = encryption == BackupEncryption.PgpPublicKey ? tbPublicKey.Text.Trim() : null,
        ProtectedPassphrase = encryption == BackupEncryption.PgpPassphrase ? GetProtectedPassphrase() : null,
        NotifyError = cbNotifyError.Checked,
        NotifyComplete = cbNotifyComplete.Checked,
      };

      if (otherNames.Contains(rule.Name))
        throw new ArgumentException($"Another backup of this rule is already named \"{rule.Name}\".");

      if (rule.InputFolders.Any(f => IsSameOrInside(rule.OutputFolder, f)))
        throw new ArgumentException("The output folder must not be inside a folder to back up.");

      if (encryption == BackupEncryption.PgpPublicKey)
        OpenPgpEncryption.FindEncryptionKey(rule.PgpPublicKey);

      BackupEngine.Validate(rule);

      return rule;
    }

    private string GetProtectedPassphrase()
    {
      if (tbPassphrase.Text.Length == 0 && tbPassphraseConfirm.Text.Length == 0 && original?.ProtectedPassphrase != null)
        return original.ProtectedPassphrase;

      if (tbPassphrase.Text.Length < 8)
        throw new ArgumentException("The passphrase must have at least 8 characters.");

      if (tbPassphrase.Text != tbPassphraseConfirm.Text)
        throw new ArgumentException("The two passphrases are not the same.");

      return PassphraseProtector.Protect(tbPassphrase.Text);
    }

    private void RefreshState()
    {
      var encryption = cbEncryption.SelectedItem is BackupEncryption e ? e : BackupEncryption.None;

      cbArchiveType.Enabled = cbCompression.Enabled = cbArchive.Checked;
      cbEncryption.Enabled = cbArchive.Checked;

      if (!cbArchive.Checked)
        cbEncryption.SelectedItem = encryption = BackupEncryption.None;

      cbCipher.Enabled = encryption != BackupEncryption.None;
      publicKeyRow.Visible = publicKeyLabel.Visible = encryption == BackupEncryption.PgpPublicKey;
      passphraseRow.Visible = passphraseLabel.Visible = encryption == BackupEncryption.PgpPassphrase;
    }

    // Called explicitly too: setting SelectedItem to the item that is already selected raises no event.
    private void FillCompressions()
    {
      cbCompression.DataSource = ((ArchiveType)cbArchiveType.SelectedItem).GetAvailableCompressions().ToList();
    }

    private void AddInputFolder()
    {
      string folder = PickFolder(null);

      if (folder != null && !lbInputs.Items.Cast<string>().Contains(folder, StringComparer.OrdinalIgnoreCase))
        lbInputs.Items.Add(folder);
    }

    private string PickFolder(string initial)
    {
      using (var dialog = new FolderBrowserDialog { InitialDirectory = initial ?? "", UseDescriptionForTitle = true, Description = "Select a folder" })
        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.SelectedPath : null;
    }

    private void LoadPublicKey()
    {
      using (var dialog = new OpenFileDialog { Filter = "OpenPGP public key (*.asc;*.pub;*.txt)|*.asc;*.pub;*.txt|All files (*.*)|*.*" })
        if (dialog.ShowDialog(this) == DialogResult.OK)
          tbPublicKey.Text = File.ReadAllText(dialog.FileName);
    }

    private void DescribePublicKey()
    {
      try
      {
        var key = OpenPgpEncryption.FindEncryptionKey(tbPublicKey.Text);
        lblPublicKeyInfo.Text = $"Encryption key {key.KeyId:X16}";
      }
      catch (Exception ex) when (ex is ArgumentException or IOException or Org.BouncyCastle.Bcpg.OpenPgp.PgpException)
      {
        lblPublicKeyInfo.Text = string.IsNullOrWhiteSpace(tbPublicKey.Text) ? "" : "No valid encryption key";
      }
    }

    private static string DescribeEncryption(BackupEncryption encryption)
    {
      return encryption switch
      {
        BackupEncryption.PgpPublicKey => "Public key",
        BackupEncryption.PgpPassphrase => "Passphrase",
        _ => "None",
      };
    }

    private static bool IsSameOrInside(string path, string folder)
    {
      if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
        return false;

      string a = Path.GetFullPath(path).TrimEnd('\\') + "\\";
      string b = Path.GetFullPath(folder).TrimEnd('\\') + "\\";

      return a.StartsWith(b, StringComparison.OrdinalIgnoreCase);
    }
  }
}
