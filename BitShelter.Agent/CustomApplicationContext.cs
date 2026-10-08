using BitShelter.Agent.Forms;
using BitShelter.Utils;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BitShelter.Agent
{
  /// <summary>
  /// Framework for running application as a tray app.
  /// </summary>
  /// <remarks>
  /// Tray app code adapted from "Creating Applications with NotifyIcon in Windows Forms", Jessica Fosler,
  /// http://windowsclient.net/articles/notifyiconapplications.aspx
  /// </remarks>
  public class CustomApplicationContext : ApplicationContext
  {
    private const string ExitConfirmMessage = "Exiting the agent won't stop the service. Use the Windows Service manager to stop BitShelter.";
    private const string ExitConfirmTitle = "Confirm";


    private IContainer components;
    private NotifyIcon trayIcon;
    private ContextMenuStrip trayContextMenuStrip;
    private BackupNotifier backupNotifier;

    public ToolStripMenuItem RunAtStartupMenuItem { get; private set; }

    /// <summary>
    /// This class should be created and passed into Application.Run( ... )
    /// </summary>
    public CustomApplicationContext()
    {
      InitializeComponent();
    }

    private void InitializeComponent()
    {
      components = new Container();

      trayIcon = new NotifyIcon(components);
      trayContextMenuStrip = new ContextMenuStrip(components);
      trayContextMenuStrip.SuspendLayout();

      // 
      // trayIcon
      trayIcon.BalloonTipText = "Double click to access settings.";
      trayIcon.BalloonTipTitle = BitShelter.Const.AppName;
      trayIcon.ContextMenuStrip = trayContextMenuStrip;
      trayIcon.Icon = AppIcon.Load();
      trayIcon.Text = BitShelter.Const.AppName;
      trayIcon.Visible = true;

      trayIcon.DoubleClick += settingsItem_Click;

      RunAtStartupMenuItem = new ToolStripMenuItem("Run at startup", null, runAtStartupItem_Click);

      // 
      // trayContextMenuStrip
      trayContextMenuStrip.Items.Add(new ToolStripMenuItem("Settings", null, settingsItem_Click));
      trayContextMenuStrip.Items.Add(RunAtStartupMenuItem);
      trayContextMenuStrip.Items.Add(new ToolStripSeparator());
      trayContextMenuStrip.Items.Add(new ToolStripMenuItem("Exit", null, exitItem_Click));
      trayContextMenuStrip.Name = "trayContextMenuStrip";

      RunAtStartupMenuItem.Checked = InstallUtils.TaskExists(Const.AppName);

      backupNotifier = new BackupNotifier(trayIcon);

      // 
      // CAC
      trayContextMenuStrip.ResumeLayout(false);
    }

    /// <summary>
    /// When the application context is disposed, dispose things like the notify icon.
    /// </summary>
    /// <param name="disposing"></param>
    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        backupNotifier?.Dispose();
        components?.Dispose();
      }

      base.Dispose(disposing);
    }

    private void settingsItem_Click(object sender, EventArgs e)
    {
      SettingsForm.DisplayInstance();
    }

    private void runAtStartupItem_Click(object sender, EventArgs e)
    {
      bool autoRun = InstallUtils.TaskExists(Const.AppName);

      if (autoRun == false)
        InstallUtils.CreateStartupTask(Const.AppName, Application.ExecutablePath);

      else
        InstallUtils.DeleteStartupTask(Const.AppName);

      RunAtStartupMenuItem.Checked = !autoRun;
    }

    private void exitItem_Click(object sender, EventArgs e)
    {
      if (MessageBox.Show(ExitConfirmMessage, ExitConfirmTitle, MessageBoxButtons.OKCancel) == DialogResult.OK)
        ExitThread();
    }

    /// <summary>
    /// If we are presently showing a form, clean it up.
    /// </summary>
    protected override void ExitThreadCore()
    {
      // before we exit, let forms clean themselves up.
      EditSnapshotRuleForm.CloseInstance();
      SettingsForm.CloseInstance();

      // should remove lingering tray icon
      trayIcon.Visible = false;

      base.ExitThreadCore();
    }
  }
}
