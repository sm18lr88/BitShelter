using BitShelter.VSS.Interop;
using System;
using System.Runtime.InteropServices;

namespace BitShelter.VSS
{
  public sealed class VssSnapshotProperties
  {
    public Guid SnapshotId { get; init; }
    public Guid SnapshotSetId { get; init; }
    public string OriginalVolumeName { get; init; }

    // For example \\?\GLOBALROOT\Device\HarddiskVolumeShadowCopy7. Files in the snapshot are read through this path.
    public string SnapshotDeviceObject { get; init; }

    public DateTime CreationTimestamp { get; init; }

    internal bool CreatedWithWriters { get; init; }

    // Copies the native properties and frees the strings that VSS allocated.
    internal static VssSnapshotProperties FromNative(ref VssSnapshotProp native)
    {
      try
      {
        return new VssSnapshotProperties
        {
          SnapshotId = native.SnapshotId,
          SnapshotSetId = native.SnapshotSetId,
          OriginalVolumeName = Marshal.PtrToStringUni(native.OriginalVolumeName),
          SnapshotDeviceObject = Marshal.PtrToStringUni(native.SnapshotDeviceObject),
          CreationTimestamp = DateTime.FromFileTime(native.CreationTimestamp),
          CreatedWithWriters = (native.SnapshotAttributes & VssNative.AttributeNoWriters) == 0,
        };
      }
      finally
      {
        VssNative.VssFreeSnapshotPropertiesInternal(ref native);
      }
    }
  }
}
