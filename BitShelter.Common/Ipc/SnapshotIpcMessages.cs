using BitShelter.Models;
using System.Collections.Generic;

namespace BitShelter.Ipc
{
  public sealed class SnapshotIpcRequest
  {
    public string Method { get; set; }

    public SnapshotRule Rule { get; set; }

    public bool DeleteSnapshots { get; set; }

    // GetBackupResults returns the results with an Id greater than this value.
    public long SinceId { get; set; }
  }

  public sealed class SnapshotIpcResponse<T>
  {
    public bool Ok { get; set; }

    public string Error { get; set; }

    public T Result { get; set; }
  }
}

