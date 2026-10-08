using BitShelter.Filters;
using BitShelter.Models;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace BitShelter.Tests.Filters
{
  public sealed class FileSystemInfoExTests
  {
    [Fact]
    public void ShouldInclude_requires_any_include_and_no_exclude_match()
    {
      var includes = new HashSet<PathFilter>
      {
        new PathFilter { FilterPatternType = FilterPatternType.Glob, Pattern = "**/*" }
      };

      var excludes = new HashSet<PathFilter>
      {
        new PathFilter { FilterPatternType = FilterPatternType.Glob, Pattern = "**/*.tmp" }
      };

      var fileOk = new FileInfo(@"C:\root\a.txt");
      var fileExcluded = new FileInfo(@"C:\root\a.tmp");

      Assert.True(fileOk.ShouldInclude(includes, excludes));
      Assert.False(fileExcluded.ShouldInclude(includes, excludes));
    }
  }
}

