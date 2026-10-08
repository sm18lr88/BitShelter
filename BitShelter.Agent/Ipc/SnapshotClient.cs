using BitShelter.Ipc;
using BitShelter.Models;
using System;
using System.Collections.Generic;
using System.IO.Pipes;

namespace BitShelter.Agent.Ipc
{
  public sealed class SnapshotClient
  {
    private readonly string serverName;
    private readonly string pipeName;
    private readonly int connectTimeoutMs;

    public SnapshotClient(string pipeName = null, string serverName = ".", TimeSpan? connectTimeout = null)
    {
      this.pipeName = string.IsNullOrWhiteSpace(pipeName) ? SnapshotIpcProtocol.PipeName : pipeName;
      this.serverName = string.IsNullOrWhiteSpace(serverName) ? "." : serverName;
      connectTimeoutMs = (int)(connectTimeout ?? SnapshotIpcProtocol.DefaultConnectTimeout).TotalMilliseconds;
    }

    public bool Ping()
    {
      return Invoke<bool>("Ping");
    }

    public IEnumerable<SnapshotRule> GetRules()
    {
      return Invoke<List<SnapshotRule>>("GetRules");
    }

    public bool AddOrUpdateRule(SnapshotRule rule)
    {
      return Invoke<bool>("AddOrUpdateRule", rule: rule);
    }

    public bool DeleteRule(SnapshotRule rule, bool deleteSnapshots)
    {
      return Invoke<bool>("DeleteRule", rule: rule, deleteSnapshots: deleteSnapshots);
    }

    public IEnumerable<BackupResult> GetBackupResults(long sinceId)
    {
      return Invoke<List<BackupResult>>("GetBackupResults", sinceId: sinceId);
    }

    private T Invoke<T>(string method, SnapshotRule rule = null, bool deleteSnapshots = false, long sinceId = 0)
    {
      var request = new SnapshotIpcRequest
      {
        Method = method,
        Rule = rule,
        DeleteSnapshots = deleteSnapshots,
        SinceId = sinceId
      };

      using (var client = new NamedPipeClientStream(serverName, pipeName, PipeDirection.InOut))
      {
        client.Connect(connectTimeoutMs);
        SnapshotIpcStream.WriteJson(client, request);
        var response = SnapshotIpcStream.ReadJson<SnapshotIpcResponse<T>>(client);

        if (response == null)
          throw new InvalidOperationException("Invalid IPC response.");
        if (!response.Ok)
          throw new InvalidOperationException(response.Error ?? "IPC request failed.");

        return response.Result;
      }
    }
  }
}
