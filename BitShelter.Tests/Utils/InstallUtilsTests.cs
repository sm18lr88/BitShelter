using BitShelter.Utils;

namespace BitShelter.Tests.Utils
{
  public sealed class InstallUtilsTests
  {
    [Fact]
    public void Startup_task_command_quotes_paths_with_spaces()
    {
      IReadOnlyList<string> args = InstallUtils.GetCreateStartupTaskArguments("BitShelter", @"C:\Program Files\BitShelter\BitShelter.Agent.exe");

      int tr = args.ToList().IndexOf("/TR");
      Assert.Equal("\"C:\\Program Files\\BitShelter\\BitShelter.Agent.exe\"", args[tr + 1]);
      Assert.Equal("BitShelter", args[args.ToList().IndexOf("/TN") + 1]);
    }
  }
}
