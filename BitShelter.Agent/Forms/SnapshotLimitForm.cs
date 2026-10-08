using BitShelter.VSS;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace BitShelter.Agent.Forms
{
  public partial class SnapshotLimitForm : Form
  {
    public SnapshotLimitForm()
    {
      InitializeComponent();

      lblMicrosoftRef.Links.Add(0, 0, "https://learn.microsoft.com/en-us/windows/win32/backup/registry-keys-for-backup-and-restore#maxshadowcopies");

      nbLimit.Value = VssUtils.GetSnapshotLimit();
    }

    private void lblMicrosoftRef_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
      Process.Start(new ProcessStartInfo(e.Link.LinkData as string) { UseShellExecute = true })?.Dispose();
    }

    private void button1_Click(object sender, EventArgs e)
    {
      VssUtils.SetSnapshotLimit((int)nbLimit.Value);
      Close();
    }
  }
}
