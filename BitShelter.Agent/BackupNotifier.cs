using BitShelter.Agent.Ipc;
using BitShelter.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BitShelter.Agent
{
  // Asks the service for new backup results once a minute and shows a tray notification
  // for each result that the rule wants to be notified about. Results from before the Agent started are not shown.
  internal sealed class BackupNotifier : IDisposable
  {
    private const int PollIntervalMs = 60_000;

    private readonly NotifyIcon trayIcon;
    private readonly Timer timer = new Timer { Interval = PollIntervalMs };
    private long? lastSeenId;
    private bool polling;

    public BackupNotifier(NotifyIcon trayIcon)
    {
      this.trayIcon = trayIcon;
      timer.Tick += async (_, _) => await PollAsync();
      timer.Start();
    }

    public void Dispose()
    {
      timer.Dispose();
    }

    private async Task PollAsync()
    {
      if (polling)
        return;

      polling = true;

      try
      {
        long sinceId = lastSeenId ?? 0;
        List<BackupResult> results = await Task.Run(() => new SnapshotClient().GetBackupResults(sinceId).ToList());

        if (results.Count == 0)
        {
          lastSeenId ??= 0;
          return;
        }

        if (lastSeenId.HasValue)
          foreach (BackupResult result in results.Where(r => r.Notify))
            Show(result);

        lastSeenId = results.Max(r => r.Id);
      }
      catch (Exception ex) when (ex is TimeoutException or IOException or UnauthorizedAccessException or InvalidOperationException)
      {
        Log.Debug(ex, "Could not get backup results from the BitShelter service");
      }
      finally
      {
        polling = false;
      }
    }

    private void Show(BackupResult result)
    {
      string title = result.Success ? "Backup completed" : "Backup failed";
      string text = $"{result.RuleName} - {result.BackupName}: {result.Message}";

      trayIcon.ShowBalloonTip(10_000, title, text, result.Success ? ToolTipIcon.Info : ToolTipIcon.Error);
    }
  }
}
