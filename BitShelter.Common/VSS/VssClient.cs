using BitShelter.Models;
using BitShelter.VSS.Interop;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BitShelter.VSS
{
  // A VSS requester for the two client-accessible contexts, which File Explorer shows under Previous Versions:
  // ClientAccessible (no writers) and ClientAccessibleWriters (applications flush their data first, as for
  // System Restore points). It follows the documented call sequence:
  // https://learn.microsoft.com/windows/win32/vss/shadow-copy-creation-details
  public sealed partial class VssClient : IDisposable
  {
    private IVssBackupComponents components;
    private bool withWriters;

    public VssClient(IUIHost host)
    {
      Host = host ?? throw new ArgumentNullException(nameof(host));
    }

    public IUIHost Host { get; }

    // Tests use this to run the call sequence against a fake IVssBackupComponents.
    internal VssClient(IUIHost host, IVssBackupComponents components, bool withWriters) : this(host)
    {
      this.components = components;
      this.withWriters = withWriters;
    }

    // Call once at process start, before any other COM use. VSS writers call back into the requester process.
    public static void InitializeProcessSecurity()
    {
      VssException.ThrowIfFailed(VssNative.InitializeComSecurity(), "CoInitializeSecurity");
    }

    public void Initialize(VssSnapshotContextInternal context)
    {
      if (context is not (VssSnapshotContextInternal.ClientAccessible or VssSnapshotContextInternal.ClientAccessibleWriters or VssSnapshotContextInternal.All))
        throw new NotSupportedException($"BitShelter does not create shadow copies in the VSS context {context}.");

      withWriters = context == VssSnapshotContextInternal.ClientAccessibleWriters;
      components = VssNative.CreateBackupComponents();
      VssException.ThrowIfFailed(components.InitializeForBackup(0), "InitializeForBackup");
      VssException.ThrowIfFailed(components.SetContext((int)context), "SetContext");

      if (withWriters)
        VssException.ThrowIfFailed(components.SetBackupState(false, false, VssNative.BackupTypeCopy, false), "SetBackupState");
    }

    public IEnumerable<Guid> CreateSnapshot(IEnumerable<string> volumes)
    {
      if (withWriters)
        GatherWriterMetadata();

      VssException.ThrowIfFailed(components.StartSnapshotSet(out Guid snapshotSetId), "StartSnapshotSet");
      Host.WriteDebug("Creating shadow copy set {0:B}", snapshotSetId);

      var snapshotIds = new List<Guid>();
      try
      {
        foreach (string volume in volumes)
        {
          VssException.ThrowIfFailed(components.AddToSnapshotSet(volume, Guid.Empty, out Guid snapshotId), $"AddToSnapshotSet({volume})");
          snapshotIds.Add(snapshotId);
        }

        if (withWriters)
          PrepareForBackup();

        Wait(components.DoSnapshotSet(out nint async), async, "DoSnapshotSet");
      }
      catch when (withWriters)
      {
        // Required between StartSnapshotSet and the end of DoSnapshotSet, so that writers can clean up.
        components.AbortBackup();
        throw;
      }

      if (withWriters)
        CompleteBackup();

      Host.WriteDebug("Created shadow copies {0}", string.Join(", ", snapshotIds.Select(id => id.ToString("B"))));
      return snapshotIds;
    }

    public IEnumerable<VssSnapshotProperties> QuerySnapshotSet()
    {
      var snapshots = new List<VssSnapshotProperties>();

      int hr = components.Query(Guid.Empty, VssNative.ObjectNone, VssNative.ObjectSnapshot, out nint enumPointer);
      VssException.ThrowIfFailed(hr, "Query");

      IVssEnumObject enumObject = VssNative.Wrap<IVssEnumObject>(enumPointer);
      if (enumObject == null)
        return snapshots;

      try
      {
        while (true)
        {
          hr = enumObject.Next(1, out VssObjectProp element, out uint fetched);
          VssException.ThrowIfFailed(hr, "IVssEnumObject.Next");

          if (hr == VssNative.S_FALSE || fetched == 0)
            return snapshots;

          snapshots.Add(VssSnapshotProperties.FromNative(ref element.Snapshot));
        }
      }
      finally
      {
        VssNative.Release(enumObject);
      }
    }

    public VssSnapshotProperties GetSnapshotProperties(Guid snapshotId)
    {
      VssException.ThrowIfFailed(components.GetSnapshotProperties(snapshotId, out VssSnapshotProp properties), $"GetSnapshotProperties({snapshotId:B})");
      return VssSnapshotProperties.FromNative(ref properties);
    }

    public bool IsVolumeSupported(string volumeName)
    {
      int hr = components.IsVolumeSupported(Guid.Empty, volumeName, out int supported);
      if (hr < 0)
      {
        Host.WriteWarning("Cannot check whether VSS supports {0}: {1}", volumeName, VssException.Describe(hr));
        return false;
      }

      return supported != 0;
    }

    public void DeleteSnapshot(Guid snapshotId)
    {
      Host.WriteDebug("Deleting shadow copy {0:B}", snapshotId);

      int hr = components.DeleteSnapshots(snapshotId, VssNative.ObjectSnapshot, 0, out _, out _);
      VssException.ThrowIfFailed(hr, $"DeleteSnapshots({snapshotId:B})");
    }

    public void Dispose()
    {
      VssNative.Release(components);
      components = null;
    }

    private static void Wait(int hr, nint asyncPointer, string operation)
    {
      VssException.ThrowIfFailed(hr, operation);

      IVssAsync async = VssNative.Wrap<IVssAsync>(asyncPointer);
      try
      {
        VssException.ThrowIfFailed(async.Wait(uint.MaxValue), operation);
        VssException.ThrowIfFailed(async.QueryStatus(out int status, 0), operation);
        VssException.ThrowIfFailed(status, operation);

        if (status == VssNative.AsyncCancelled)
          throw new OperationCanceledException($"{operation} was cancelled.");
      }
      finally
      {
        VssNative.Release(async);
      }
    }
  }
}
