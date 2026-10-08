using System;
using System.Drawing;
using System.IO;

namespace BitShelter.Agent
{
  // The BitShelter icon. The file is next to the Agent executable, so the tray and the windows use one copy.
  internal static class AppIcon
  {
    public const string FileName = "BitShelter.ico";

    public static Icon Load(int size = 0)
    {
      string path = Path.Combine(AppContext.BaseDirectory, FileName);

      return size > 0 ? new Icon(path, size, size) : new Icon(path);
    }
  }
}
