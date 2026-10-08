using BitShelter.Models;
using BitShelter.Service.Config;
using BitShelter.Service.Data;
using BitShelter.Service.Ipc;
using BitShelter.Service.Scheduler;
using Serilog;
using System;
using System.Collections.Generic;

namespace BitShelter.Service
{
  partial class BitShelter
  {
    private const string ConfigInitErrorMsg = @"A fatal error occured: Configuration could not be loaded.
Please make sure BitShelter execution privileges, {0} folder permissions, and files access are correct.";

    private SnapshotPipeServer SnapshotPipeServer { get; set; }

    public void OnPostInitialize()
    {
      try
      {
        AppDataSecurity.Apply(Const.GetAppDataFolderPath());

        ConfigMgr.Instance.Initialize(this);

        SnapshotPipeServer = new SnapshotPipeServer(new SnapshotService());
        SnapshotPipeServer.Start();
      }
      catch (Exception ex)
      {
        string err = String.Format(ConfigInitErrorMsg, Const.GetAppDataFolderPath());

        Log.Error(ex, err);
        Log.CloseAndFlush();

        throw new Exception(err, ex);
      }
    }

    public void OnPreInitialize()
    {
      QuartzScheduler.Instance.Start().Wait();
    }

    public void Shutdown()
    {
      SnapshotPipeServer?.Dispose();

      QuartzScheduler.Instance.Shutdown().Wait();
    }
  }
}
