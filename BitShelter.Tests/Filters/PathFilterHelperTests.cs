using BitShelter.IO;
using BitShelter.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace BitShelter.Tests.Filters
{
  public sealed class PathFilterHelperTests
  {
    [Fact]
    public void TraverseDirectoryWithFilter_returns_matching_files_and_invokes_callback()
    {
      using var tmp = new TempDir();
      Directory.CreateDirectory(Path.Combine(tmp.Path, "sub"));

      File.WriteAllText(Path.Combine(tmp.Path, "a.txt"), "a");
      File.WriteAllText(Path.Combine(tmp.Path, "a.bin"), "b");
      File.WriteAllText(Path.Combine(tmp.Path, "sub", "b.txt"), "c");
      File.WriteAllText(Path.Combine(tmp.Path, "sub", "secret.txt"), "d");

      var includes = new HashSet<PathFilter>
      {
        new PathFilter { FilterPatternType = FilterPatternType.Glob, Pattern = "**/*.txt" }
      };

      var excludes = new HashSet<PathFilter>
      {
        new PathFilter { FilterPatternType = FilterPatternType.Glob, Pattern = "**/secret.txt" }
      };

      var callbacks = new List<string>();
      FileInfo[] files = PathFilterHelper
        .TraverseDirectoryWithFilter(tmp.Path, includes, excludes, fi => callbacks.Add(fi.Name))
        .OrderBy(f => f.FullName)
        .ToArray();

      Assert.Equal(new[] { "a.txt", "b.txt" }, files.Select(f => f.Name).OrderBy(x => x).ToArray());
      Assert.Equal(new[] { "a.txt", "b.txt" }, callbacks.OrderBy(x => x).ToArray());
    }

    private sealed class TempDir : IDisposable
    {
      public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "BitShelter.Tests", Guid.NewGuid().ToString("N"));

      public TempDir()
      {
        Directory.CreateDirectory(Path);
      }

      public void Dispose()
      {
        try { Directory.Delete(Path, recursive: true); } catch { }
      }
    }
  }
}

