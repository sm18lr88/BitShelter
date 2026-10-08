using BitShelter.Agent.SingleApp;
using Serilog;
using System;
using System.Windows.Forms;

namespace BitShelter.Agent
{
  static class Program
  {
    [STAThread]
    static void Main()
    {
      if (!SingleInstance.Start())
        return;

      var appHost = new DefaultAppHost();

      try
      {
        AppInit.Initialize(appHost);

        Log.Information("BitShelter Agent starting");

        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        Application.Run(new CustomApplicationContext());

        Log.Information("BitShelter Agent stopping");

        AppInit.Shutdown(appHost);
      }
      catch (Exception ex)
      {
        Log.Fatal(ex, "Unrecoverable exception");
        MessageBox.Show(ex.Message, "BitShelter Agent terminated unexpectedly", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
      finally
      {
        SingleInstance.Stop();
      }
    }
  }
}
