using BitShelter.Agent.Ipc;
using Serilog;
using System;
using System.IO;

namespace BitShelter
{
  // Connection to the BitShelter service. While the service is unreachable, the form shows a banner and retries.
  public partial class SettingsForm
  {
    private const int RetryDelay = 5;
    private const string RetryText = "Cannot reach the BitShelter service.\nMake sure the \"BitShelter\" Windows service is running.\n\nRetrying in {0}...";

    private int retryCountdown;
    protected SnapshotClient SnapshotClient { get; private set; }

    private void CleanupSnapshotClient()
    {
      SnapshotClient = null;
    }

    private void SetupSnapshotClient()
    {
      CleanupSnapshotClient();

      SnapshotClient = new SnapshotClient();
    }

    private void ConnectSnapshotClient()
    {
      SetupSnapshotClient();

      try
      {
        if (SnapshotClient.Ping())
        {
          StopConnectionRetry();
          RefreshDataGrid();
          return;
        }
      }
      catch (Exception ex) when (ex is TimeoutException or IOException or UnauthorizedAccessException or InvalidOperationException)
      {
        Log.Debug(ex, "BitShelter service is not reachable yet");
      }

      if (!retryConnTimer.Enabled)
        StartConnectionRetry();
    }



    //
    // Connection retry

    private void StartConnectionRetry()
    {
      plSvcConnection.Visible = true;
      plSvcConnection.Enabled = true;
      plSvcConnection.BringToFront();

      retryCountdown = RetryDelay;
      lblConnWait.Text = String.Format(RetryText, retryCountdown);
      retryConnTimer.Start();
    }

    private void StopConnectionRetry()
    {
      plSvcConnection.Visible = false;
      plSvcConnection.Enabled = false;

      retryConnTimer.Stop();
    }

    private void retryConnTimer_Tick(object sender, EventArgs e)
    {
      retryCountdown = (retryCountdown <= 1) ? RetryDelay : retryCountdown - 1;

      lblConnWait.Text = String.Format(RetryText, retryCountdown);

      if (retryCountdown == RetryDelay)
        ConnectSnapshotClient();
    }
  }
}
