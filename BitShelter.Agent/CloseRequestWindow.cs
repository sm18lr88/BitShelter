using System;
using System.Windows.Forms;

namespace BitShelter.Agent
{
  // A hidden top-level window that ends the Agent when it gets WM_CLOSE. The tray Agent has no other window that
  // handles WM_CLOSE, and the installer (util:CloseApplication in Package.wxs) sends WM_CLOSE to stop the Agent
  // before it replaces or removes the Agent files.
  internal sealed class CloseRequestWindow : NativeWindow, IDisposable
  {
    internal const string Caption = "BitShelter Agent";
    private const int WM_CLOSE = 0x0010;

    private readonly Action onClose;

    public CloseRequestWindow(Action onClose)
    {
      this.onClose = onClose ?? throw new ArgumentNullException(nameof(onClose));
      CreateHandle(new CreateParams { Caption = Caption });
    }

    protected override void WndProc(ref Message m)
    {
      if (m.Msg == WM_CLOSE)
      {
        onClose();
        return;
      }

      base.WndProc(ref m);
    }

    public void Dispose()
    {
      DestroyHandle();
    }
  }
}
