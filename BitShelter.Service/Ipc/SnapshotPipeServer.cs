using BitShelter.Ipc;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace BitShelter.Service.Ipc
{
  internal sealed class SnapshotPipeServer : IDisposable
  {
    private readonly CancellationTokenSource cts = new CancellationTokenSource();
    private readonly ISnapshotService snapshotService;
    private readonly string pipeName;
    private Task acceptLoop;

    public SnapshotPipeServer(ISnapshotService snapshotService, string pipeName = null)
    {
      this.snapshotService = snapshotService ?? throw new ArgumentNullException(nameof(snapshotService));
      this.pipeName = string.IsNullOrWhiteSpace(pipeName) ? SnapshotIpcProtocol.PipeName : pipeName;
    }

    public void Start()
    {
      if (acceptLoop != null)
        return;

      acceptLoop = Task.Run(() => AcceptLoopAsync(cts.Token));
    }

    public void Dispose()
    {
      cts.Cancel();
      try { acceptLoop?.Wait(TimeSpan.FromSeconds(2)); } catch { }
      cts.Dispose();
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
      while (!cancellationToken.IsCancellationRequested)
      {
        NamedPipeServerStream server = null;
        try
        {
          server = CreateServer();

          using (cancellationToken.Register(() =>
          {
            try { server.Dispose(); } catch { }
          }))
          {
            await server.WaitForConnectionAsync().ConfigureAwait(false);
          }

          if (!server.IsConnected)
          {
            server.Dispose();
            continue;
          }

          _ = Task.Run(() => HandleClient(server), cancellationToken);
        }
        // Stopping disposes the waiting pipe. Windows reports this as ObjectDisposedException or as IOException ("The pipe has been ended").
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
          try { server?.Dispose(); } catch { }
          break;
        }
        catch (Exception ex)
        {
          try { server?.Dispose(); } catch { }
          Log.Error(ex, "SnapshotPipeServer accept loop error");
          await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }
      }
    }

    // The service runs as LocalSystem and can create and delete shadow copies, so only the
    // service account and elevated administrators (the Agent requires elevation) may connect.
    internal static PipeSecurity CreatePipeSecurity()
    {
      var security = new PipeSecurity();

      using (WindowsIdentity serviceIdentity = WindowsIdentity.GetCurrent())
        security.AddAccessRule(new PipeAccessRule(serviceIdentity.User, PipeAccessRights.FullControl, AccessControlType.Allow));

      var admins = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
      security.AddAccessRule(new PipeAccessRule(admins, PipeAccessRights.ReadWrite, AccessControlType.Allow));

      return security;
    }

    private NamedPipeServerStream CreateServer()
    {
      return NamedPipeServerStreamAcl.Create(
        pipeName,
        PipeDirection.InOut,
        NamedPipeServerStream.MaxAllowedServerInstances,
        PipeTransmissionMode.Byte,
        PipeOptions.Asynchronous,
        0,
        0,
        CreatePipeSecurity());
    }

    private void HandleClient(NamedPipeServerStream server)
    {
      try
      {
        HandleRequest(server);
      }
      catch (Exception ex)
      {
        Log.Warning(ex, "SnapshotPipeServer failed to handle a client request");
      }
    }

    private void HandleRequest(NamedPipeServerStream server)
    {
      using (server)
      {
        SnapshotIpcRequest request = SnapshotIpcStream.ReadJson<SnapshotIpcRequest>(server);
        if (request == null || string.IsNullOrWhiteSpace(request.Method))
        {
          SnapshotIpcStream.WriteJson(server, new SnapshotIpcResponse<bool> { Ok = false, Error = "Invalid request.", Result = false });
          return;
        }

        switch (request.Method)
        {
          case "Ping":
            SnapshotIpcStream.WriteJson(server, new SnapshotIpcResponse<bool> { Ok = true, Result = snapshotService.Ping() });
            break;

          case "GetRules":
            SnapshotIpcStream.WriteJson(server, new SnapshotIpcResponse<List<Models.SnapshotRule>>
            {
              Ok = true,
              Result = new List<Models.SnapshotRule>(snapshotService.GetRules())
            });
            break;

          case "AddOrUpdateRule":
            SnapshotIpcStream.WriteJson(server, new SnapshotIpcResponse<bool> { Ok = true, Result = snapshotService.AddOrUpdateRule(request.Rule) });
            break;

          case "DeleteRule":
            SnapshotIpcStream.WriteJson(server, new SnapshotIpcResponse<bool> { Ok = true, Result = snapshotService.DeleteRule(request.Rule, request.DeleteSnapshots) });
            break;

          case "GetBackupResults":
            SnapshotIpcStream.WriteJson(server, new SnapshotIpcResponse<List<Models.BackupResult>>
            {
              Ok = true,
              Result = new List<Models.BackupResult>(snapshotService.GetBackupResults(request.SinceId))
            });
            break;

          default:
            SnapshotIpcStream.WriteJson(server, new SnapshotIpcResponse<bool> { Ok = false, Error = "Unknown method.", Result = false });
            break;
        }
      }
    }
  }
}
