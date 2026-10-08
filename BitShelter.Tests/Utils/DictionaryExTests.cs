using BitShelter.Utils;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BitShelter.Tests.Utils
{
  public sealed class DictionaryExTests
  {
    [Fact]
    public void SafeGet_dictionary_returns_default_when_missing()
    {
      var d = new Dictionary<string, int> { ["a"] = 1 };
      Assert.Equal(1, d.SafeGet("a"));
      Assert.Equal(0, d.SafeGet("missing"));
      Assert.Equal(42, d.SafeGet("missing", 42));
    }

    [Fact]
    public void SafeGet_concurrent_dictionary_returns_default_when_missing()
    {
      var d = new ConcurrentDictionary<string, int>();
      d["a"] = 1;
      Assert.Equal(1, d.SafeGet("a"));
      Assert.Equal(0, d.SafeGet("missing"));
      Assert.Equal(42, d.SafeGet("missing", 42));
    }

    [Fact]
    public void ToConcurrentDictionary_builds_dictionary_from_source()
    {
      var source = new[] { "a", "bb" };
      var dict = source.ToConcurrentDictionary(s => s.Length);
      Assert.Equal("a", dict[1]);
      Assert.Equal("bb", dict[2]);

      var dict2 = source.ToConcurrentDictionary(s => s, s => s.Length);
      Assert.Equal(1, dict2["a"]);
      Assert.Equal(2, dict2["bb"]);

      var dict3 = new[] { new KeyValuePair<int, string>(1, "x") }.ToConcurrentDictionary();
      Assert.Equal("x", dict3[1]);
      Assert.True(dict3.Keys.SequenceEqual(new[] { 1 }));
    }
  }
}

