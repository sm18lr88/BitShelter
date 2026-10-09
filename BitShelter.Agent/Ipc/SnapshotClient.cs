using BitShelter.Ipc;
using BitShelter.Models;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Security.Principal;

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

    // Tests turn this off, because their in-process server belongs to the test user.
    internal bool RequireServiceOwner { get; init; } = true;

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

        if (RequireServiceOwner)
          VerifyServiceOwner(client);

        SnapshotIpcStream.WriteJson(client, request);
        var response = SnapshotIpcStream.ReadJson<SnapshotIpcResponse<T>>(client);

        if (response == null)
          throw new InvalidOperationException("Invalid IPC response.");
        if (!response.Ok)
          throw new InvalidOperationException(response.Error ?? "IPC request failed.");

        return response.Result;
      }
    }

    // While the service is stopped, any local user can create a pipe with this name. The requests carry rules
    // with passphrases that any local user can unprotect (machine-scope DPAPI), so the Agent talks only to a pipe
    // that LocalSystem owns. The service sets itself as the owner (SnapshotPipeServer.CreatePipeSecurity).
    private void VerifyServiceOwner(NamedPipeClientStream client)
    {
      var owner = client.GetAccessControl().GetOwner(typeof(SecurityIdentifier)) as SecurityIdentifier;

      if (owner == null || !owner.IsWellKnown(WellKnownSidType.LocalSystemSid))
        throw new UnauthorizedAccessException($"The pipe {pipeName} does not belong to the BitShelter service (owner: {owner?.Value ?? "unknown"}).");
    }
  }
}
