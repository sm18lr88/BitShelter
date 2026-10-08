using System.Xml.Linq;

namespace BitShelter.Tests.Common
{
  // The MSI ships the runtime packages, so each one must be named in THIRD-PARTY-NOTICES.txt with its license.
  public sealed class ThirdPartyNoticesTests
  {
    [Fact]
    public void Every_runtime_package_is_listed_in_the_notices()
    {
      string root = FindRepositoryRoot();
      string notices = File.ReadAllText(Path.Combine(root, "THIRD-PARTY-NOTICES.txt")).ReplaceLineEndings("\n");

      IEnumerable<string> runtimePackages = XDocument.Load(Path.Combine(root, "Directory.Packages.props"))
        .Descendants("ItemGroup")
        .Where(g => (string?)g.Attribute("Label") is "Runtime" or "Logging")
        .Elements("PackageVersion")
        .Select(p => (string)p.Attribute("Include")! + " " + (string)p.Attribute("Version")!);

      Assert.All(runtimePackages, package => Assert.Contains("* " + package + "\n", notices));
    }

    private static string FindRepositoryRoot()
    {
      for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        if (File.Exists(Path.Combine(dir.FullName, "BitShelter.slnx")))
          return dir.FullName;

      throw new DirectoryNotFoundException("BitShelter.slnx was not found above the test output folder.");
    }
  }
}
