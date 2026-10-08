using System.Collections.Generic;
using System.Diagnostics;

namespace BitShelter.Utils
{
  public static class InstallUtils
  {
    public static bool CreateStartupTask(string taskName, string filePath)
    {
      return RunSchTasks(GetCreateStartupTaskArguments(taskName, filePath));
    }

    public static bool DeleteStartupTask(string taskName)
    {
      return RunSchTasks(new[] { "/Delete", "/F", "/TN", taskName });
    }

    public static bool TaskExists(string taskName)
    {
      return RunSchTasks(new[] { "/Query", "/TN", taskName });
    }

    // schtasks parses /TR as a command line, so a path with spaces (e.g. Program Files) must carry its own quotes.
    public static IReadOnlyList<string> GetCreateStartupTaskArguments(string taskName, string filePath)
    {
      return new[] { "/Create", "/F", "/SC", "ONLOGON", "/TN", taskName, "/TR", $"\"{filePath}\"", "/RL", "HIGHEST" };
    }

    private static bool RunSchTasks(IEnumerable<string> arguments)
    {
      var startInfo = new ProcessStartInfo("schtasks")
      {
        UseShellExecute = false,
        CreateNoWindow = true
      };

      foreach (string argument in arguments)
        startInfo.ArgumentList.Add(argument);

      using (Process p = Process.Start(startInfo))
      {
        p.WaitForExit();

        return p.ExitCode == 0;
      }
    }
  }
}
