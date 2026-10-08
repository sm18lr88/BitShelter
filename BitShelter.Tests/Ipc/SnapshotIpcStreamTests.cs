using BitShelter.Ipc;
using System;
using System.IO;
using Xunit;

namespace BitShelter.Tests.Ipc
{
  public sealed class SnapshotIpcStreamTests
  {
    [Fact]
    public void WriteJson_then_ReadJson_roundtrips_payload()
    {
      using var ms = new MemoryStream();
      SnapshotIpcStream.WriteJson(ms, new SnapshotIpcRequest { Method = "Ping" });

      ms.Position = 0;
      SnapshotIpcRequest request = SnapshotIpcStream.ReadJson<SnapshotIpcRequest>(ms);

      Assert.NotNull(request);
      Assert.Equal("Ping", request.Method);
    }

    [Fact]
    public void WriteJson_writes_length_prefix_equal_to_payload_byte_count()
    {
      using var ms = new MemoryStream();
      var payload = new SnapshotIpcRequest { Method = "Ping" };

      SnapshotIpcStream.WriteJson(ms, payload);

      byte[] bytes = ms.ToArray();
      Assert.True(bytes.Length >= 4);

      int len = BitConverter.ToInt32(bytes, 0);
      Assert.Equal(bytes.Length - 4, len);
    }

    [Fact]
    public void ReadJson_negative_length_throws()
    {
      using var ms = new MemoryStream();
      ms.Write(BitConverter.GetBytes(-1));
      ms.Position = 0;

      Assert.Throws<InvalidDataException>(() => SnapshotIpcStream.ReadJson<SnapshotIpcRequest>(ms));
    }

    [Fact]
    public void ReadJson_length_above_limit_throws_before_allocating()
    {
      using var ms = new MemoryStream();
      ms.Write(BitConverter.GetBytes(SnapshotIpcProtocol.MaxMessageBytes + 1));
      ms.Position = 0;

      Assert.Throws<InvalidDataException>(() => SnapshotIpcStream.ReadJson<SnapshotIpcRequest>(ms));
    }

    [Fact]
    public void WriteJson_payload_above_limit_throws_without_writing()
    {
      using var ms = new MemoryStream();
      var payload = new SnapshotIpcRequest { Method = new string('x', SnapshotIpcProtocol.MaxMessageBytes) };

      Assert.Throws<InvalidDataException>(() => SnapshotIpcStream.WriteJson(ms, payload));
      Assert.Equal(0, ms.Length);
    }

    [Fact]
    public void ReadJson_truncated_payload_throws()
    {
      using var ms = new MemoryStream();
      ms.Write(BitConverter.GetBytes(10));
      ms.Write(new byte[] { 1, 2, 3 });
      ms.Position = 0;

      Assert.Throws<EndOfStreamException>(() => SnapshotIpcStream.ReadJson<SnapshotIpcRequest>(ms));
    }
  }
}
