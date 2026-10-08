using BitShelter.Backup;

namespace BitShelter.Tests.Backup
{
  public sealed class BackupRetentionTests
  {
    private static BackupEntry Entry(string name, int day, long size = 10)
    {
      return new BackupEntry { Path = name, CreatedAt = new DateTime(2026, 1, day), SizeBytes = size };
    }

    [Fact]
    public void SelectOverCount_keeps_the_newest_backups()
    {
      var entries = new[] { Entry("a", 1), Entry("b", 2), Entry("c", 3), Entry("d", 4) };

      Assert.Equal(new[] { "b", "a" }, BackupRetention.SelectOverCount(entries, 2, keepPath: "d").Select(e => e.Path));
      Assert.Empty(BackupRetention.SelectOverCount(entries, 0, keepPath: "d"));
    }

    [Fact]
    public void SelectOverTotalSize_deletes_oldest_first_but_never_the_new_backup()
    {
      var entries = new[] { Entry("old", 1, 50), Entry("mid", 2, 30), Entry("new", 3, 40) };

      Assert.Equal(new[] { "old" }, BackupRetention.SelectOverTotalSize(entries, 80, keepPath: "new").Select(e => e.Path));
      Assert.Equal(new[] { "old", "mid" }, BackupRetention.SelectOverTotalSize(entries, 10, keepPath: "new").Select(e => e.Path));
      Assert.Empty(BackupRetention.SelectOverTotalSize(entries, 0, keepPath: "new"));
    }
  }
}
