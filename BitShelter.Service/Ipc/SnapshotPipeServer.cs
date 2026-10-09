using BitShelter.Ipc;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace BitShelter.Service.Ipc
{
  internal sealed class SnapshotPipeServer : IDisposable
  {
    // Several listening instances, so that concurrent clients (for example the Agent UI and its
    // backup notifier) do not wait for one accept loop to create the next pipe instance.
    internal const int ListenerCount = 4;

    private readonly CancellationTokenSource cts = new CancellationTokenSource();
    private readonly ISnapshotService snapshotService;
    private readonly string pipeName;
    private Task[] acceptLoops;

    public SnapshotPipeServer(ISnapshotService snapshotService, string pipeName = null)
    {
      this.snapshotService = snapshotService ?? throw new ArgumentNullException(nameof(snapshotService));
      this.pipeName = string.IsNullOrWhiteSpace(pipeName) ? SnapshotIpcProtocol.PipeName : pipeName;
    }

    public void Start()
    {
      if (acceptLoops != null)
        return;

      acceptLoops = new Task[ListenerCount];
      for (int i = 0; i < ListenerCount; i++)
        acceptLoops[i] = Task.Run(() => AcceptLoopAsync(cts.Token));
    }

    public void Dispose()
    {
      cts.Cancel();
      try { if (acceptLoops != null) Task.WaitAll(acceptLoops, TimeSpan.FromSeconds(2)); } catch { }
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
    // The service account is also the owner: the Agent checks it to detect a pipe that another user created.
    internal static PipeSecurity CreatePipeSecurity()
    {
      var security = new PipeSecurity();

      using (WindowsIdentity serviceIdentity = WindowsIdentity.GetCurrent())
      {
        security.SetOwner(serviceIdentity.User);
        security.AddAccessRule(new PipeAccessRule(serviceIdentity.User, PipeAccessRights.FullControl, AccessControlType.Allow));
      }

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
      // A client that closes the pipe before it sends a complete request (for example a tool that only checks
      // that the pipe exists) is not a server error.
      catch (EndOfStreamException)
      {
        Log.Debug("A SnapshotPipeServer client disconnected before it sent a complete request");
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
