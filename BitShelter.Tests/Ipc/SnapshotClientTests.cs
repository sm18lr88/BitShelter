using BitShelter.Agent.Ipc;
using System;
using Xunit;

namespace BitShelter.Tests.Ipc
{
  public sealed class SnapshotClientTests
  {
    [Fact]
    public void Ping_throws_when_service_unavailable()
    {
      string pipeName = $"BitShelter.Tests.Missing.{Guid.NewGuid():N}";
      var client = new SnapshotClient(pipeName: pipeName, connectTimeout: TimeSpan.FromMilliseconds(25));

      Assert.ThrowsAny<Exception>(() => client.Ping());
    }
  }
}

