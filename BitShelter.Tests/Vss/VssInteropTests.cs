using BitShelter.VSS;
using BitShelter.VSS.Interop;
using System.Runtime.InteropServices;

namespace BitShelter.Tests.Vss
{
  // The native layouts must match vss.h exactly; a wrong size or offset corrupts memory instead of failing cleanly.
  public sealed class VssInteropTests
  {
    [Fact]
    public void Snapshot_properties_match_the_native_VSS_SNAPSHOT_PROP_layout()
    {
      Assert.True(Environment.Is64BitProcess);
      Assert.Equal(128, Marshal.SizeOf<VssSnapshotProp>());
      Assert.Equal(40, (int)Marshal.OffsetOf<VssSnapshotProp>(nameof(VssSnapshotProp.SnapshotDeviceObject)));
      Assert.Equal(88, (int)Marshal.OffsetOf<VssSnapshotProp>(nameof(VssSnapshotProp.ProviderId)));
      Assert.Equal(112, (int)Marshal.OffsetOf<VssSnapshotProp>(nameof(VssSnapshotProp.CreationTimestamp)));
    }

    [Fact]
    public void Object_properties_match_the_native_VSS_OBJECT_PROP_layout()
    {
      Assert.Equal(136, Marshal.SizeOf<VssObjectProp>());
      Assert.Equal(8, (int)Marshal.OffsetOf<VssObjectProp>(nameof(VssObjectProp.Snapshot)));
    }

    [Fact]
    public void Vss_errors_are_reported_by_name()
    {
      var ex = new VssException("DoSnapshotSet", unchecked((int)0x80042317));

      Assert.Equal(unchecked((int)0x80042317), ex.HResult);
      Assert.Contains("VSS_E_MAXIMUM_NUMBER_OF_SNAPSHOTS_REACHED", ex.Message);
    }
  }
}
