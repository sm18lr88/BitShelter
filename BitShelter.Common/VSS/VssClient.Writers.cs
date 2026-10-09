using BitShelter.VSS.Interop;
using System;
using System.Runtime.InteropServices;

namespace BitShelter.VSS
{
  // The writer steps for ClientAccessibleWriters. BitShelter does not select components (SetBackupState with
  // bSelectComponents = false), so every writer on the volumes takes part, as for System Restore points.
  public sealed partial class VssClient
  {
    private void GatherWriterMetadata()
    {
      int freeResult;
      try
      {
        Wait(components.GatherWriterMetadata(out nint async), async, "GatherWriterMetadata");
      }
      finally
      {
        freeResult = components.FreeWriterMetadata();
      }

      VssException.ThrowIfFailed(freeResult, "FreeWriterMetadata");
    }

    private void PrepareForBackup()
    {
      Wait(components.PrepareForBackup(out nint async), async, "PrepareForBackup");
      WarnAboutFailedWriters("PrepareForBackup");
    }

    // The shadow copies exist once DoSnapshotSet succeeds, so a failure here must not hide their IDs from the
    // caller, which records them for pruning. Otherwise the job retries, creates a second set, and never deletes
    // the first one. BackupComplete runs even when the writer status check fails.
    private void CompleteBackup()
    {
      WarnIfFails(() => WarnAboutFailedWriters("DoSnapshotSet"));
      WarnIfFails(() => Wait(components.BackupComplete(out nint async), async, "BackupComplete"));
    }

    private void WarnIfFails(Action step)
    {
      try
      {
        step();
      }
      catch (Exception ex)
      {
        Host.WriteWarning("The shadow copies were created, but a later VSS step failed: {0}", ex.Message);
      }
    }

    // A failed writer does not stop the shadow copy: its files are then only crash-consistent, like a snapshot
    // without writers. Each failure is logged so that a broken application writer is visible.
    private void WarnAboutFailedWriters(string phase)
    {
      Wait(components.GatherWriterStatus(out nint async), async, "GatherWriterStatus");

      try
      {
        VssException.ThrowIfFailed(components.GetWriterStatusCount(out uint count), "GetWriterStatusCount");

        for (uint i = 0; i < count; i++)
        {
          int hr = components.GetWriterStatus(i, out _, out Guid writerId, out nint bstrName, out int state, out int failure);
          VssException.ThrowIfFailed(hr, "GetWriterStatus");

          string name = bstrName == 0 ? "(unnamed)" : Marshal.PtrToStringBSTR(bstrName);
          Marshal.FreeBSTR(bstrName);

          if (state >= VssNative.WriterStateFirstFailure)
            Host.WriteWarning("VSS writer {0} {1:B} failed after {2}: state {3}, {4}", name, writerId, phase, state, VssException.Describe(failure));
        }
      }
      finally
      {
        components.FreeWriterStatus();
      }
    }
  }
}
