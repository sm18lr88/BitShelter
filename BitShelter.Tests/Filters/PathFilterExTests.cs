using BitShelter.Filters;
using BitShelter.Models;
using System;
using Xunit;

namespace BitShelter.Tests.Filters
{
  public sealed class PathFilterExTests
  {
    [Fact]
    public void Apply_glob_matches_expected_paths()
    {
      var filter = new PathFilter
      {
        FilterPatternType = FilterPatternType.Glob,
        Pattern = "**/*.txt"
      };

      Assert.True(filter.Apply(@"C:\root\a.txt"));
      Assert.True(filter.Apply(@"C:\root\sub\b.txt"));
      Assert.False(filter.Apply(@"C:\root\a.bin"));
    }

    [Fact]
    public void Apply_regex_matches_expected_paths()
    {
      var filter = new PathFilter
      {
        FilterPatternType = FilterPatternType.Regexp,
        Pattern = @"\\sub\\.*\.txt$"
      };

      Assert.True(filter.Apply(@"C:\root\sub\b.txt"));
      Assert.False(filter.Apply(@"C:\root\sub\b.bin"));
      Assert.False(filter.Apply(@"C:\root\other\b.txt"));
    }

    [Fact]
    public void Apply_unknown_pattern_type_throws()
    {
      var filter = new PathFilter
      {
        FilterPatternType = (FilterPatternType)999,
        Pattern = "x"
      };

      var ex = Assert.Throws<ArgumentException>(() => filter.Apply(@"C:\x"));
      Assert.Equal("pathFilter.FilterPatternType", ex.ParamName);
    }
  }
}
