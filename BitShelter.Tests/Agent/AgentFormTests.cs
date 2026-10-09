using BitShelter.Agent.Forms;
using BitShelter.Encryption;
using BitShelter.Models;
using BitShelter.Models.Enums;
using SharpCompress.Common;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace BitShelter.Tests.Agent
{
  // Opens the real Agent forms. The rule editor lists volumes through VSS, so run from an elevated shell.
  public sealed class AgentFormTests
  {
    private const BindingFlags NonPublic = BindingFlags.Instance | BindingFlags.NonPublic;

    [Fact]
    public void Rule_editor_opens_and_keeps_advanced_and_backup_settings()
    {
      RunOnStaThread(() =>
      {
        SnapshotRule rule = Generate(Create<EditSnapshotRuleForm>((SnapshotRule?)null));
        rule.PruningStrategy = PruningStrategy.Local;
        rule.VssContext = VssSnapshotContextInternal.ClientAccessibleWriters;
        rule.RetryRestartVSSService = true;
        rule.MaxRetryCount = 2;
        rule.BackupEnabled = true;
        rule.BackupRules = new List<BackupRule> { Backup() };

        SnapshotRule saved = Generate(Create<EditSnapshotRuleForm>(rule));

        Assert.Equal(PruningStrategy.Local, saved.PruningStrategy);
        Assert.Equal(VssSnapshotContextInternal.ClientAccessibleWriters, saved.VssContext);
        Assert.True(saved.RetryRestartVSSService);
        Assert.Equal(2, saved.MaxRetryCount);
        Assert.True(saved.BackupEnabled);
        Assert.Equal("Docs", Assert.Single(saved.BackupRules).Name);
      });
    }

    [Fact]
    public void New_rule_defaults_to_snapshots_every_4_hours_without_VSS_restart_or_total_backup_limit()
    {
      RunOnStaThread(() =>
      {
        SnapshotRule rule = Generate(Create<EditSnapshotRuleForm>((SnapshotRule?)null));

        Assert.Equal(Freq.Daily, rule.Freq);
        Assert.Equal(DailyFreq.Every, rule.DailyFreq);
        Assert.Equal(4 * 60, rule.DailyFreqEvery);
        Assert.False(rule.RetryRestartVSSService);
        Assert.Equal(VssSnapshotContextInternal.ClientAccessible, rule.VssContext);
        Assert.Equal(3, rule.MaxRetryCount);
        Assert.Equal(PruningStrategy.Global, rule.PruningStrategy);
        Assert.Equal((1, Timespan.Week), (rule.LifeTimeValue, rule.LifeTimeUnit));
        Assert.False(rule.BackupEnabled);
        Assert.Equal(0, rule.BackupTotalMaxSize);
      });
    }

    [Theory]
    [InlineData(true, -1, false)]
    [InlineData(true, 1, true)]
    [InlineData(false, -1, true)]
    public void Rule_editor_rejects_an_end_date_before_the_start_date_only_when_the_end_date_is_on(bool endEnabled, int endOffsetDays, bool valid)
    {
      RunOnStaThread(() =>
      {
        SnapshotRule rule = Generate(Create<EditSnapshotRuleForm>((SnapshotRule?)null));
        rule.PeriodStart = DateTime.Today;
        rule.PeriodEndEnabled = endEnabled;
        rule.PeriodEnd = DateTime.Today.AddDays(endOffsetDays);

        using var form = Create<EditSnapshotRuleForm>(rule);

        Assert.Equal(valid, (bool)typeof(EditSnapshotRuleForm).GetMethod("ValidatePeriod", NonPublic)!.Invoke(form, null)!);
      });
    }

    [Theory]
    [InlineData(ArchiveType.Tar, CompressionType.GZip)]
    [InlineData(ArchiveType.Zip, CompressionType.Deflate)]
    public void Backup_editor_opens_and_keeps_the_backup_settings(ArchiveType archiveType, CompressionType compressionType)
    {
      RunOnStaThread(() =>
      {
        BackupRule original = Backup();
        original.ArchiveType = archiveType;
        original.CompressionType = compressionType;
        using var form = Create<EditBackupRuleForm>(original, Array.Empty<string>());

        var saved = (BackupRule)typeof(EditBackupRuleForm).GetMethod("BuildRule", NonPublic)!.Invoke(form, null)!;

        Assert.Equal(original.InputFolders, saved.InputFolders);
        Assert.Equal(original.OutputFolder, saved.OutputFolder);
        Assert.Equal(archiveType, saved.ArchiveType);
        Assert.Equal(compressionType, saved.CompressionType);
        Assert.Equal(BackupEncryption.PgpPassphrase, saved.Encryption);
        Assert.Equal(EncryptionAlgorithm.Twofish_CFB, saved.EncryptionType);
        Assert.Equal(original.ProtectedPassphrase, saved.ProtectedPassphrase);
        Assert.Equal(new[] { "**/*.tmp" }, saved.FilterExcludes.Select(f => f.Pattern));
        Assert.Equal((3, 1, 14), (saved.Every, saved.Offset, saved.MaxBackupCount));
      });
    }

    [Fact]
    public void Backup_editor_offers_only_Deflate_and_None_for_an_encrypted_zip()
    {
      RunOnStaThread(() =>
      {
        BackupRule original = Backup();
        original.ArchiveType = ArchiveType.Zip;
        original.CompressionType = CompressionType.BZip2;
        using var form = Create<EditBackupRuleForm>(original, Array.Empty<string>());

        var compressions = (ComboBox)typeof(EditBackupRuleForm).GetField("cbCompression", NonPublic)!.GetValue(form)!;
        var saved = (BackupRule)typeof(EditBackupRuleForm).GetMethod("BuildRule", NonPublic)!.Invoke(form, null)!;

        Assert.Equal(new[] { CompressionType.None, CompressionType.Deflate }, compressions.Items.Cast<CompressionType>());
        Assert.Equal(CompressionType.Deflate, saved.CompressionType);
      });
    }

    private static BackupRule Backup()
    {
      return new BackupRule
      {
        Name = "Docs",
        InputFolders = new HashSet<string> { Path.GetTempPath().TrimEnd('\\') },
        OutputFolder = @"D:\Backups",
        FilterIncludes = new HashSet<PathFilter>(),
        FilterExcludes = new HashSet<PathFilter> { new PathFilter { FilterPatternType = FilterPatternType.Glob, Pattern = "**/*.tmp" } },
        Every = 3,
        Offset = 1,
        MaxBackupCount = 14,
        CompressionEnabled = true,
        ArchiveType = ArchiveType.Tar,
        CompressionType = CompressionType.GZip,
        Encryption = BackupEncryption.PgpPassphrase,
        EncryptionType = EncryptionAlgorithm.Twofish_CFB,
        ProtectedPassphrase = PassphraseProtector.Protect("a long passphrase"),
      };
    }

    private static T Create<T>(params object?[] args) where T : Form
    {
      return (T)Activator.CreateInstance(typeof(T), NonPublic, null, args, null)!;
    }

    private static SnapshotRule Generate(EditSnapshotRuleForm form)
    {
      using (form)
        return (SnapshotRule)typeof(EditSnapshotRuleForm).GetMethod("GenerateSchedule", NonPublic)!.Invoke(form, null)!;
    }

    // WinForms controls need a single-threaded apartment.
    private static void RunOnStaThread(Action action)
    {
      ExceptionDispatchInfo? failure = null;
      var thread = new Thread(() =>
      {
        try { action(); }
        catch (Exception ex) { failure = ExceptionDispatchInfo.Capture(ex is TargetInvocationException { InnerException: { } inner } ? inner : ex); }
      });

      thread.SetApartmentState(ApartmentState.STA);
      thread.Start();
      thread.Join();
      failure?.Throw();
    }
  }
}
