using BitShelter.Backup;

namespace BitShelter.Tests.Backup
{
  public sealed class BackupScheduleTests
  {
    [Theory]
    [InlineData(0, 0, 1, true)]
    [InlineData(5, 0, 1, true)]
    [InlineData(0, 0, 3, true)]
    [InlineData(1, 0, 3, false)]
    [InlineData(3, 0, 3, true)]
    [InlineData(1, 2, 3, false)]
    [InlineData(2, 2, 3, true)]
    [InlineData(5, 2, 3, true)]
    [InlineData(6, 2, 3, false)]
    [InlineData(4, 0, 0, true)]
    public void IsDue_runs_on_offset_then_every_n_snapshots(long snapshotNumber, int offset, int every, bool expected)
    {
      Assert.Equal(expected, BackupSchedule.IsDue(snapshotNumber, offset, every));
    }
  }
}
