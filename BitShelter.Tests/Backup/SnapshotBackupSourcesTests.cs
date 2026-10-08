using BitShelter.Service.Backup;

namespace BitShelter.Tests.Backup
{
  public sealed class SnapshotBackupSourcesTests
  {
    [Fact]
    public void MapToSnapshot_replaces_the_volume_root_with_the_snapshot_device()
    {
      string mapped = SnapshotBackupSources.MapToSnapshot(@"D:\Docs\Work", @"D:\", @"\?\GLOBALROOT\Device\HarddiskVolumeShadowCopy7");

      Assert.Equal(@"\?\GLOBALROOT\Device\HarddiskVolumeShadowCopy7\Docs\Work", mapped);
    }

    [Fact]
    public void MapToSnapshot_rejects_a_path_on_another_volume()
    {
      Assert.Throws<ArgumentException>(() => SnapshotBackupSources.MapToSnapshot(@"E:\Docs", @"D:\", @"\?\GLOBALROOT\Device\HarddiskVolumeShadowCopy7"));
    }
  }
}
