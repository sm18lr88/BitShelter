using BitShelter.Backup;
using BitShelter.Encryption;
using BitShelter.Models;
using SharpCompress.Common;
using System.Formats.Tar;
using System.IO.Compression;

namespace BitShelter.Tests.Backup
{
  public sealed class BackupEngineTests : IDisposable
  {
    private static readonly DateTime Now = new DateTime(2026, 10, 8, 12, 0, 0);

    private readonly string root = Directory.CreateTempSubdirectory("bitshelter-backup-tests-").FullName;
    private readonly string input;
    private readonly string output;

    public BackupEngineTests()
    {
      input = Path.Combine(root, "input");
      output = Path.Combine(root, "output");

      Write("docs/report.docx", "report");
      Write("docs/notes.txt", "notes");
      Write("docs/cache/big.tmp", "temporary");
      Write("photo.jpg", "photo");
    }

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void Zip_backup_contains_filtered_files_under_the_source_label()
    {
      BackupRule backup = Rule(includes: new[] { "**/*.docx", "**/*.txt", "**/*.tmp" }, excludes: new[] { "**/cache" });

      BackupResult result = Run(backup);

      Assert.True(result.Success);
      Assert.Equal(2, result.FileCount);
      Assert.EndsWith(".zip", result.OutputPath);
      using ZipArchive zip = ZipFile.OpenRead(result.OutputPath);
      string label = BackupNaming.GetSourceLabel(input);
      Assert.Equal(new[] { label + "/docs/notes.txt", label + "/docs/report.docx" }, zip.Entries.Select(e => e.FullName).Order());
    }

    [Fact]
    public void Tar_gz_backup_can_be_read_by_standard_tools()
    {
      BackupRule backup = Rule();
      backup.ArchiveType = ArchiveType.Tar;
      backup.CompressionType = CompressionType.GZip;

      BackupResult result = Run(backup);

      Assert.EndsWith(".tar.gz", result.OutputPath);
      using var gzip = new GZipStream(File.OpenRead(result.OutputPath), CompressionMode.Decompress);
      using var tar = new TarReader(gzip);
      var names = new List<string>();
      for (TarEntry? entry = tar.GetNextEntry(); entry != null; entry = tar.GetNextEntry())
        names.Add(entry.Name);
      Assert.Equal(4, names.Count);
      Assert.Contains(names, n => n.EndsWith("/photo.jpg"));
    }

    [Fact]
    public void Folder_backup_copies_the_tree()
    {
      BackupRule backup = Rule();
      backup.CompressionEnabled = false;

      BackupResult result = Run(backup);

      Assert.True(Directory.Exists(result.OutputPath));
      string copied = Path.Combine(result.OutputPath, BackupNaming.GetSourceLabel(input).Replace('/', '\\'), "docs", "notes.txt");
      Assert.Equal("notes", File.ReadAllText(copied));
    }

    [Fact]
    public void Backup_over_size_limit_fails_and_leaves_nothing_behind()
    {
      Write("large.bin", new string('x', 3 * 1024 * 1024));
      BackupRule backup = Rule();
      backup.CompressionType = CompressionType.None;
      backup.MaxSizeMB = 1;

      Assert.Throws<BackupSizeLimitException>(() => Run(backup));

      string folder = BackupNaming.GetBackupFolder(output, "Rule", backup.Name);
      Assert.Empty(Directory.EnumerateFileSystemEntries(folder));
    }

    [Fact]
    public void Old_backups_beyond_the_count_limit_are_deleted()
    {
      BackupRule backup = Rule();
      backup.MaxBackupCount = 2;

      for (int i = 0; i < 3; i++)
        Run(backup, Now.AddHours(i));

      string folder = BackupNaming.GetBackupFolder(output, "Rule", backup.Name);
      Assert.Equal(new[] { "20261008-130000.zip", "20261008-140000.zip" }, Directory.GetFiles(folder).Select(Path.GetFileName).Order());
    }

    [Fact]
    public void Passphrase_encrypted_backup_decrypts_to_the_zip_archive()
    {
      BackupRule backup = Rule();
      backup.Encryption = BackupEncryption.PgpPassphrase;
      backup.EncryptionType = EncryptionAlgorithm.AES256_CFB;
      backup.ProtectedPassphrase = PassphraseProtector.Protect("correct horse battery");

      BackupResult result = Run(backup);

      Assert.EndsWith(".zip.gpg", result.OutputPath);
      byte[] zipBytes = OpenPgpTestHelper.DecryptWithPassphrase(File.ReadAllBytes(result.OutputPath), "correct horse battery", out string name);
      Assert.Equal(Path.GetFileNameWithoutExtension(result.OutputPath), name);
      using var zip = new ZipArchive(new MemoryStream(zipBytes));
      Assert.Equal(4, zip.Entries.Count);
    }

    [Fact]
    public void Public_key_encrypted_backup_decrypts_with_the_private_key()
    {
      var key = OpenPgpTestHelper.CreateKey();
      BackupRule backup = Rule();
      backup.Encryption = BackupEncryption.PgpPublicKey;
      backup.EncryptionType = EncryptionAlgorithm.Camellia256_CFB;
      backup.PgpPublicKey = key.ArmoredPublicKey;

      BackupResult result = Run(backup);

      byte[] zipBytes = OpenPgpTestHelper.DecryptWithKey(File.ReadAllBytes(result.OutputPath), key.SecretRing, key.KeyPassphrase, out _);
      using var zip = new ZipArchive(new MemoryStream(zipBytes));
      Assert.Equal(4, zip.Entries.Count);
    }

    [Fact]
    public void Encryption_without_archive_is_rejected()
    {
      BackupRule backup = Rule();
      backup.CompressionEnabled = false;
      backup.Encryption = BackupEncryption.PgpPassphrase;

      Assert.Throws<ArgumentException>(() => BackupEngine.Validate(backup));
    }

    private BackupResult Run(BackupRule backup, DateTime? now = null)
    {
      var rule = new SnapshotRule { Id = 1, Name = "Rule", BackupRules = new List<BackupRule> { backup } };
      var sources = new[] { BackupSource.FromFolder(input, input) };

      return BackupEngine.Run(rule, backup, sources, now ?? Now, CancellationToken.None);
    }

    private BackupRule Rule(string[]? includes = null, string[]? excludes = null)
    {
      return new BackupRule
      {
        Name = "Docs",
        InputFolders = new HashSet<string> { input },
        OutputFolder = output,
        FilterIncludes = Filters(includes),
        FilterExcludes = Filters(excludes),
        CompressionEnabled = true,
        ArchiveType = ArchiveType.Zip,
        CompressionType = CompressionType.Deflate,
      };
    }

    private static HashSet<PathFilter> Filters(string[]? patterns)
    {
      return (patterns ?? Array.Empty<string>()).Select(p => new PathFilter { FilterPatternType = FilterPatternType.Glob, Pattern = p }).ToHashSet();
    }

    private void Write(string relativePath, string content)
    {
      string path = Path.Combine(input, relativePath);
      Directory.CreateDirectory(Path.GetDirectoryName(path)!);
      File.WriteAllText(path, content);
    }
  }
}
