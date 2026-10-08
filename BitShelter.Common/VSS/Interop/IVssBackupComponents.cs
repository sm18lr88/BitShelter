using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace BitShelter.VSS.Interop
{
  // IVssBackupComponents from vsbackup.h. Every method is declared in vtable order, including the ones that
  // BitShelter does not call, because COM dispatches by slot. Interface out-parameters are raw pointers that the
  // caller wraps with VssNative.Wrap, so that each COM object can be released deterministically.
  [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
  [Guid("665c1d5f-c218-414d-a05d-7fef5f9d5c86")]
  internal partial interface IVssBackupComponents
  {
    [PreserveSig] int GetWriterComponentsCount(out uint count);
    [PreserveSig] int GetWriterComponents(uint writer, out nint writerComponents);
    [PreserveSig] int InitializeForBackup(nint bstrXml);
    [PreserveSig] int SetBackupState([MarshalAs(UnmanagedType.U1)] bool selectComponents, [MarshalAs(UnmanagedType.U1)] bool backupBootableSystemState, int backupType, [MarshalAs(UnmanagedType.U1)] bool partialFileSupport);
    [PreserveSig] int InitializeForRestore(nint bstrXml);
    [PreserveSig] int SetRestoreState(int restoreType);
    [PreserveSig] int GatherWriterMetadata(out nint async);
    [PreserveSig] int GetWriterMetadataCount(out uint count);
    [PreserveSig] int GetWriterMetadata(uint writer, out Guid instanceId, out nint metadata);
    [PreserveSig] int FreeWriterMetadata();
    [PreserveSig] int AddComponent(Guid instanceId, Guid writerId, int componentType, string logicalPath, string componentName);
    [PreserveSig] int PrepareForBackup(out nint async);
    [PreserveSig] int AbortBackup();
    [PreserveSig] int GatherWriterStatus(out nint async);
    [PreserveSig] int GetWriterStatusCount(out uint count);
    [PreserveSig] int FreeWriterStatus();
    [PreserveSig] int GetWriterStatus(uint writer, out Guid instanceId, out Guid writerId, out nint bstrWriter, out int state, out int failure);
    [PreserveSig] int SetBackupSucceeded(Guid instanceId, Guid writerId, int componentType, string logicalPath, string componentName, [MarshalAs(UnmanagedType.U1)] bool succeeded);
    [PreserveSig] int SetBackupOptions(Guid writerId, int componentType, string logicalPath, string componentName, string backupOptions);
    [PreserveSig] int SetSelectedForRestore(Guid writerId, int componentType, string logicalPath, string componentName, [MarshalAs(UnmanagedType.U1)] bool selectedForRestore);
    [PreserveSig] int SetRestoreOptions(Guid writerId, int componentType, string logicalPath, string componentName, string restoreOptions);
    [PreserveSig] int SetAdditionalRestores(Guid writerId, int componentType, string logicalPath, string componentName, [MarshalAs(UnmanagedType.U1)] bool additionalRestores);
    [PreserveSig] int SetPreviousBackupStamp(Guid writerId, int componentType, string logicalPath, string componentName, string previousBackupStamp);
    [PreserveSig] int SaveAsXML(out nint bstrXml);
    [PreserveSig] int BackupComplete(out nint async);
    [PreserveSig] int AddAlternativeLocationMapping(Guid writerId, int componentType, string logicalPath, string componentName, string path, string filespec, [MarshalAs(UnmanagedType.U1)] bool recursive, string destination);
    [PreserveSig] int AddRestoreSubcomponent(Guid writerId, int componentType, string logicalPath, string componentName, string subComponentLogicalPath, string subComponentName, [MarshalAs(UnmanagedType.U1)] bool repair);
    [PreserveSig] int SetFileRestoreStatus(Guid writerId, int componentType, string logicalPath, string componentName, int status);
    [PreserveSig] int AddNewTarget(Guid writerId, int componentType, string logicalPath, string componentName, string path, string fileName, [MarshalAs(UnmanagedType.U1)] bool recursive, string alternatePath);
    [PreserveSig] int SetRangesFilePath(Guid writerId, int componentType, string logicalPath, string componentName, uint partialFile, string rangesFile);
    [PreserveSig] int PreRestore(out nint async);
    [PreserveSig] int PostRestore(out nint async);
    [PreserveSig] int SetContext(int context);
    [PreserveSig] int StartSnapshotSet(out Guid snapshotSetId);
    [PreserveSig] int AddToSnapshotSet(string volumeName, Guid providerId, out Guid snapshotId);
    [PreserveSig] int DoSnapshotSet(out nint async);
    [PreserveSig] int DeleteSnapshots(Guid sourceObjectId, int sourceObjectType, int forceDelete, out int deletedSnapshots, out Guid nondeletedSnapshotId);
    [PreserveSig] int ImportSnapshots(out nint async);
    [PreserveSig] int BreakSnapshotSet(Guid snapshotSetId);
    [PreserveSig] int GetSnapshotProperties(Guid snapshotId, out VssSnapshotProp properties);
    [PreserveSig] int Query(Guid queriedObjectId, int queriedObjectType, int returnedObjectsType, out nint enumObject);
    [PreserveSig] int IsVolumeSupported(Guid providerId, string volumeName, out int supportedByThisProvider);
    [PreserveSig] int DisableWriterClasses(nint writerClassIds, uint count);
    [PreserveSig] int EnableWriterClasses(nint writerClassIds, uint count);
    [PreserveSig] int DisableWriterInstances(nint writerInstanceIds, uint count);
    [PreserveSig] int ExposeSnapshot(Guid snapshotId, string pathFromRoot, int attributes, string expose, out nint exposed);
    [PreserveSig] int RevertToSnapshot(Guid snapshotId, int forceDismount);
    [PreserveSig] int QueryRevertStatus(string volume, out nint async);
  }

  // IVssAsync from vss.h.
  [GeneratedComInterface]
  [Guid("507C37B4-CF5B-4e95-B0AF-14EB9767467E")]
  internal partial interface IVssAsync
  {
    [PreserveSig] int Cancel();
    [PreserveSig] int Wait(uint milliseconds);
    [PreserveSig] int QueryStatus(out int result, nint reserved);
  }

  // IVssEnumObject from vss.h. Next is called with one element at a time.
  [GeneratedComInterface]
  [Guid("AE1C7110-2F60-11d3-8A39-00C04F72D8E3")]
  internal partial interface IVssEnumObject
  {
    [PreserveSig] int Next(uint count, out VssObjectProp element, out uint fetched);
    [PreserveSig] int Skip(uint count);
    [PreserveSig] int Reset();
    [PreserveSig] int Clone(ref nint enumObject);
  }
}
