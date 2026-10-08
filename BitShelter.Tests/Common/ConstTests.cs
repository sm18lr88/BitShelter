using BitShelter;
using System;
using System.IO;
using Xunit;

namespace BitShelter.Tests.Common
{
  public sealed class ConstTests
  {
    [Fact]
    public void GetAppDataFolderPath_is_under_common_app_data_and_ends_with_app_name()
    {
      string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
      string path = Const.GetAppDataFolderPath();

      Assert.StartsWith(baseDir, path, StringComparison.OrdinalIgnoreCase);
      Assert.EndsWith(Path.DirectorySeparatorChar + Const.AppName, path, StringComparison.OrdinalIgnoreCase);
    }
  }
}

