using BitShelter.Agent;
using System.Runtime.InteropServices;

namespace BitShelter.Tests.Agent
{
  // The installer stops the tray Agent by sending WM_CLOSE to its top-level windows (util:CloseApplication).
  public sealed partial class CloseRequestWindowTests
  {
    private const int WM_CLOSE = 0x0010;

    [Fact]
    public void A_close_message_to_the_Agent_top_level_window_ends_the_Agent()
    {
      bool closed = false;
      bool topLevel = false;
      var thread = new Thread(() =>
      {
        using var window = new CloseRequestWindow(() => closed = true);
        topLevel = FindWindow(null, CloseRequestWindow.Caption) == window.Handle;
        SendMessage(window.Handle, WM_CLOSE, 0, 0);
      });

      thread.SetApartmentState(ApartmentState.STA);
      thread.Start();
      thread.Join();

      Assert.True(topLevel);
      Assert.True(closed);
    }

    [LibraryImport("user32.dll", EntryPoint = "FindWindowW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint FindWindow(string? className, string windowName);

    [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
    private static partial nint SendMessage(nint window, int message, nint wParam, nint lParam);
  }
}
