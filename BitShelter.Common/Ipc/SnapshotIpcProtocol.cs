using System;

namespace BitShelter.Ipc
{
  public static class SnapshotIpcProtocol
  {
    public const string PipeName = "BitShelter.SnapshotService";

    public static readonly TimeSpan DefaultConnectTimeout = TimeSpan.FromSeconds(2);

    public const int MaxMessageBytes = 16 * 1024 * 1024;
  }
}

