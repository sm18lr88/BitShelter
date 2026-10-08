using BitShelter.Models;

namespace BitShelter.Tests.Models
{
  public sealed class SnapshotRuleVssTests
  {
    // Rules saved by earlier versions can hold any VSS context; they must map to one of the two supported ones.
    [Theory]
    [InlineData(VssSnapshotContextInternal.ClientAccessible, VssSnapshotContextInternal.ClientAccessible)]
    [InlineData(VssSnapshotContextInternal.ClientAccessibleWriters, VssSnapshotContextInternal.ClientAccessibleWriters)]
    [InlineData(VssSnapshotContextInternal.Backup, VssSnapshotContextInternal.ClientAccessibleWriters)]
    [InlineData(VssSnapshotContextInternal.AppRollback, VssSnapshotContextInternal.ClientAccessibleWriters)]
    [InlineData(VssSnapshotContextInternal.FileShareBackup, VssSnapshotContextInternal.ClientAccessible)]
    [InlineData(VssSnapshotContextInternal.NasRollback, VssSnapshotContextInternal.ClientAccessible)]
    public void Saved_context_maps_to_a_supported_snapshot_context(VssSnapshotContextInternal saved, VssSnapshotContextInternal expected)
    {
      var rule = new SnapshotRule { VssContext = saved };

      Assert.Equal(expected, rule.SnapshotContext);
    }
  }
}
