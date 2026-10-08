using BitShelter.Backup;
using SharpCompress.Common;

namespace BitShelter.Tests.Backup
{
  public sealed class BackupNamingTests
  {
    [Theory]
    [InlineData(ArchiveType.Zip, CompressionType.Deflate, false, ".zip")]
    [InlineData(ArchiveType.Zip, CompressionType.Deflate, true, ".zip.gpg")]
    [InlineData(ArchiveType.Tar, CompressionType.None, false, ".tar")]
    [InlineData(ArchiveType.Tar, CompressionType.GZip, false, ".tar.gz")]
    [InlineData(ArchiveType.Tar, CompressionType.BZip2, true, ".tar.bz2.gpg")]
    [InlineData(ArchiveType.Tar, CompressionType.LZip, false, ".tar.lz")]
    public void GetExtension_matches_archive_compression_and_encryption(ArchiveType archive, CompressionType compression, bool encrypted, string expected)
    {
      Assert.Equal(expected, BackupNaming.GetExtension(archive: true, archive, compression, encrypted));
    }

    [Fact]
    public void Folder_backups_have_no_extension()
    {
      Assert.Equal("", BackupNaming.GetExtension(archive: false, ArchiveType.Zip, CompressionType.None, encrypted: false));
    }

    [Fact]
    public void Timestamp_roundtrips_and_partial_entries_are_ignored()
    {
      var createdAt = new DateTime(2026, 10, 8, 13, 5, 9);
      string name = BackupNaming.GetEntryName(createdAt, ".zip");

      Assert.Equal("20261008-130509.zip", name);
      Assert.True(BackupNaming.TryParseTimestamp(name, out DateTime parsed));
      Assert.Equal(createdAt, parsed);
      Assert.False(BackupNaming.TryParseTimestamp(name + BackupNaming.PartialSuffix, out _));
      Assert.False(BackupNaming.TryParseTimestamp("notes.txt", out _));
    }

    [Fact]
    public void Names_are_made_safe_for_the_file_system()
    {
      Assert.Equal("a_b_c", BackupNaming.Sanitize("a:b?c"));
      Assert.Equal("_", BackupNaming.Sanitize("  ..  "));
      Assert.Equal("D/Docs/Work", BackupNaming.GetSourceLabel(@"D:\Docs\Work\"));
      Assert.Equal(@"C:\out\Daily - Docs", BackupNaming.GetBackupFolder(@"C:\out", "Daily", "Docs"));
    }
  }
}
