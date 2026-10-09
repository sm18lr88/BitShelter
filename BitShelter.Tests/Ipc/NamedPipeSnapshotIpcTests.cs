using BitShelter.Agent.Ipc;
using BitShelter.Ipc;
using BitShelter.Models;
using BitShelter.Service.Ipc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BitShelter.Tests.Ipc
{
  public sealed class NamedPipeSnapshotIpcTests
  {
    private const int ConnectTimeoutMs = 2000;

    private sealed class TestSnapshotService : ISnapshotService
    {
      private readonly ConcurrentDictionary<long, SnapshotRule> rules = new();

      public bool Ping() => true;

      public IEnumerable<SnapshotRule> GetRules()
      {
        return rules.Values.OrderBy(r => r.Id).ToArray();
      }

      public bool AddOrUpdateRule(SnapshotRule rule)
      {
        if (rule == null)
          return false;

        rules[rule.Id] = rule;
        return true;
      }

      public bool DeleteRule(SnapshotRule rule, bool deleteSnapshots)
      {
        if (rule == null)
          return false;

        return rules.TryRemove(rule.Id, out _);
      }

      public IEnumerable<BackupResult> GetBackupResults(long sinceId)
      {
        return new[]
        {
          new BackupResult { Id = 1, BackupName = "old", Success = true },
          new BackupResult { Id = 2, BackupName = "new", Success = false, Message = "Disk full", Notify = true },
        }.Where(r => r.Id > sinceId);
      }
    }

    [Fact]
    public void SnapshotClient_can_roundtrip_over_named_pipe()
    {
      string pipeName = $"{SnapshotIpcProtocol.PipeName}.tests.{Guid.NewGuid():N}";

      using var server = new SnapshotPipeServer(new TestSnapshotService(), pipeName);
      server.Start();

      var client = new SnapshotClient(pipeName: pipeName) { RequireServiceOwner = false };

      Assert.True(client.Ping());

      Assert.Empty(client.GetRules());

      var rule = new SnapshotRule
      {
        Id = 123,
        Name = "Test rule",
        Enabled = true
      };

      Assert.True(client.AddOrUpdateRule(rule));
      SnapshotRule[] rules = client.GetRules().ToArray();
      Assert.Single(rules);
      Assert.Equal(123, rules[0].Id);
      Assert.Equal("Test rule", rules[0].Name);

      rule.Name = "Updated rule";
      Assert.True(client.AddOrUpdateRule(rule));
      Assert.Equal("Updated rule", client.GetRules().Single().Name);

      Assert.True(client.DeleteRule(rule, deleteSnapshots: false));
      Assert.Empty(client.GetRules());
    }

    [Fact]
    public void SnapshotClient_gets_backup_results_newer_than_the_given_id()
    {
      string pipeName = $"{SnapshotIpcProtocol.PipeName}.tests.{Guid.NewGuid():N}";

      using var server = new SnapshotPipeServer(new TestSnapshotService(), pipeName);
      server.Start();

      BackupResult result = Assert.Single(new SnapshotClient(pipeName: pipeName) { RequireServiceOwner = false }.GetBackupResults(sinceId: 1));

      Assert.Equal("new", result.BackupName);
      Assert.Equal("Disk full", result.Message);
      Assert.True(result.Notify);
    }

    [Fact]
    public void SnapshotClient_refuses_a_pipe_that_LocalSystem_does_not_own()
    {
      string pipeName = $"{SnapshotIpcProtocol.PipeName}.tests.{Guid.NewGuid():N}";

      // The in-process server belongs to the test user, as a pipe that another user created would.
      using var server = new SnapshotPipeServer(new TestSnapshotService(), pipeName);
      server.Start();

      Assert.Throws<UnauthorizedAccessException>(() => new SnapshotClient(pipeName: pipeName).Ping());
    }

    [Fact]
    public void A_client_that_disconnects_without_a_request_does_not_stop_the_server()
    {
      string pipeName = $"{SnapshotIpcProtocol.PipeName}.tests.{Guid.NewGuid():N}";

      using var server = new SnapshotPipeServer(new TestSnapshotService(), pipeName);
      server.Start();

      using (var silent = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut))
        silent.Connect(ConnectTimeoutMs);

      Assert.True(new SnapshotClient(pipeName: pipeName) { RequireServiceOwner = false }.Ping());
    }

    [Fact]
    public void Unknown_method_returns_error_response()
    {
      string pipeName = $"{SnapshotIpcProtocol.PipeName}.tests.{Guid.NewGuid():N}";

      using var server = new SnapshotPipeServer(new TestSnapshotService(), pipeName);
      server.Start();

      using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
      // Connect waits until the server listens on the pipe, so the test does not poll.
      client.Connect(ConnectTimeoutMs);

      SnapshotIpcStream.WriteJson(client, new SnapshotIpcRequest { Method = "DoesNotExist" });
      SnapshotIpcResponse<bool> response = SnapshotIpcStream.ReadJson<SnapshotIpcResponse<bool>>(client);

      Assert.NotNull(response);
      Assert.False(response.Ok);
      Assert.Equal("Unknown method.", response.Error);
    }

    [Fact]
    public void Invalid_request_returns_error_response()
    {
      string pipeName = $"{SnapshotIpcProtocol.PipeName}.tests.{Guid.NewGuid():N}";

      using var server = new SnapshotPipeServer(new TestSnapshotService(), pipeName);
      server.Start();

      using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
      // Connect waits until the server listens on the pipe, so the test does not poll.
      client.Connect(ConnectTimeoutMs);

      SnapshotIpcStream.WriteJson(client, new SnapshotIpcRequest { Method = "" });
      SnapshotIpcResponse<bool> response = SnapshotIpcStream.ReadJson<SnapshotIpcResponse<bool>>(client);

      Assert.NotNull(response);
      Assert.False(response.Ok);
      Assert.Equal("Invalid request.", response.Error);
    }

    [Fact]
    public async Task Multiple_clients_can_ping_concurrently()
    {
      string pipeName = $"{SnapshotIpcProtocol.PipeName}.tests.{Guid.NewGuid():N}";

      using var server = new SnapshotPipeServer(new TestSnapshotService(), pipeName);
      server.Start();

      // A long connect timeout: the test checks that every concurrent request succeeds, not how fast the machine is.
      var clients = Enumerable.Range(0, 20).Select(_ => new SnapshotClient(pipeName: pipeName, connectTimeout: TimeSpan.FromSeconds(60)) { RequireServiceOwner = false }).ToArray();

      Assert.True(clients[0].Ping());

      bool[] results = await Task.WhenAll(clients.Select(c => Task.Run(() => c.Ping())));
      Assert.All(results, Assert.True);
    }
  }
}
