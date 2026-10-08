using BitShelter.Service.Scheduler;
using BitShelter.Utils;
using Serilog;
using Serilog.Events;
using System;
using System.Diagnostics;
using System.ServiceProcess;

namespace BitShelter.Service
{
  public partial class BitShelter : ServiceBase, IAppHost
  {
    public BitShelter()
    {
      InitializeComponent();
    }

    protected override void OnStart(string[] args)
    {
      try
      {
        AppInit.Initialize(this);

        Log.Information("BitShelter Service started");
      }
      catch (Exception ex)
      {
        Log.Error(ex, "Failed to start scheduling service");
        throw;
      }
    }

    protected override void OnStop()
    {
      try
      {
        Log.Information("BitShelter Service stopping");

        AppInit.Shutdown(this);
      }
      catch (Exception ex)
      {
        Log.Error(ex, "Failed to stop scheduling service");
        throw;
      }
    }
  }
}
