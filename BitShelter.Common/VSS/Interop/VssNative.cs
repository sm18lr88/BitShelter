using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace BitShelter.VSS.Interop
{
  // Flat exports of VssApi.dll and the VSS structures, as declared in vsbackup.h and vss.h (Windows SDK 10.0.28000).
  internal static partial class VssNative
  {
    // VSS_SNAPSHOT_CONTEXT
    public const int ContextBackup = 0x0;
    public const int ContextClientAccessibleWriters = 0x0D;
    public const int ContextClientAccessible = 0x1D;
    public const int ContextAll = unchecked((int)0xFFFFFFFF);

    // VSS_OBJECT_TYPE
    public const int ObjectNone = 1;
    public const int ObjectSnapshot = 3;

    // VSS_BACKUP_TYPE. A copy backup does not update the backup history of applications (for example, log truncation).
    public const int BackupTypeCopy = 5;

    // VSS_VOLSNAP_ATTR_NO_WRITERS
    public const int AttributeNoWriters = 0x10;

    // VSS_WRITER_STATE values from VSS_WS_FAILED_AT_IDENTIFY up are failures.
    public const int WriterStateFirstFailure = 6;

    // IVssAsync::QueryStatus results
    public const int AsyncPending = 0x00042309;
    public const int AsyncFinished = 0x0004230A;
    public const int AsyncCancelled = 0x0004230B;

    public const int S_OK = 0;
    public const int S_FALSE = 1;

    private const int RPC_E_TOO_LATE = unchecked((int)0x80010119);

    // Requester COM security as documented for Windows 8 and later without remote file shares:
    // https://learn.microsoft.com/windows/win32/vss/security-considerations-for-requestors
    // Must run once per process, before any COM call. Returns the HRESULT; RPC_E_TOO_LATE means it was already set.
    public static int InitializeComSecurity()
    {
      const int COINIT_MULTITHREADED = 0x0;
      const int RPC_C_AUTHN_LEVEL_PKT_PRIVACY = 6;
      const int RPC_C_IMP_LEVEL_IDENTIFY = 2;
      const int EOAC_NONE = 0;

      CoInitializeEx(0, COINIT_MULTITHREADED);
      int hr = CoInitializeSecurity(0, -1, 0, 0, RPC_C_AUTHN_LEVEL_PKT_PRIVACY, RPC_C_IMP_LEVEL_IDENTIFY, 0, EOAC_NONE, 0);
      return hr == RPC_E_TOO_LATE ? S_OK : hr;
    }

    [LibraryImport("ole32.dll")]
    private static partial int CoInitializeEx(nint reserved, int coInit);

    [LibraryImport("ole32.dll")]
    private static partial int CoInitializeSecurity(nint securityDescriptor, int authServiceCount, nint authServices, nint reserved1, int authnLevel, int impLevel, nint authList, int capabilities, nint reserved3);

    private static readonly StrategyBasedComWrappers ComWrappers = new StrategyBasedComWrappers();

    // CreateVssBackupComponents is a C++ inline wrapper around this export.
    [LibraryImport("vssapi.dll")]
    private static partial int CreateVssBackupComponentsInternal(out nint backupComponents);

    // VssFreeSnapshotProperties is a C++ inline wrapper around this export.
    [LibraryImport("vssapi.dll")]
    public static partial void VssFreeSnapshotPropertiesInternal(ref VssSnapshotProp properties);

    public static IVssBackupComponents CreateBackupComponents()
    {
      VssException.ThrowIfFailed(CreateVssBackupComponentsInternal(out nint pointer), "CreateVssBackupComponents");
      return Wrap<IVssBackupComponents>(pointer);
    }

    // Takes ownership of a COM pointer. UniqueInstance lets Release() free the native object deterministically.
    public static T Wrap<T>(nint pointer) where T : class
    {
      if (pointer == 0)
        return null;

      try
      {
        return (T)ComWrappers.GetOrCreateObjectForComInstance(pointer, CreateObjectFlags.UniqueInstance);
      }
      finally
      {
        Marshal.Release(pointer);
      }
    }

    public static void Release(object comObject)
    {
      (comObject as ComObject)?.FinalRelease();
    }
  }

  // VSS_SNAPSHOT_PROP. Strings are owned by VSS until VssFreeSnapshotPropertiesInternal.
  [StructLayout(LayoutKind.Sequential)]
  internal struct VssSnapshotProp
  {
    public Guid SnapshotId;
    public Guid SnapshotSetId;
    public int SnapshotsCount;
    public nint SnapshotDeviceObject;
    public nint OriginalVolumeName;
    public nint OriginatingMachine;
    public nint ServiceMachine;
    public nint ExposedName;
    public nint ExposedPath;
    public Guid ProviderId;
    public int SnapshotAttributes;
    public long CreationTimestamp;
    public int Status;
  }

  // VSS_OBJECT_PROP: a VSS_OBJECT_TYPE tag followed by a union of VSS_SNAPSHOT_PROP (128 bytes) and VSS_PROVIDER_PROP (72 bytes).
  [StructLayout(LayoutKind.Explicit, Size = 136)]
  internal struct VssObjectProp
  {
    [FieldOffset(0)]
    public int Type;

    [FieldOffset(8)]
    public VssSnapshotProp Snapshot;
  }
}
