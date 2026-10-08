using BitShelter.Utils;
using System;
using Xunit;

namespace BitShelter.Tests.Utils
{
  public sealed class StringTableTests
  {
    [Fact]
    public void Constructor_requires_same_count_labels_and_values()
    {
      Assert.Throws<ArgumentException>(() => new StringTable(new[] { "a" }, Array.Empty<object>()));
    }

    [Fact]
    public void Add_accepts_null_value()
    {
      var t = new StringTable();
      t.Add("x", null);
      Assert.Equal(1, t.Count);
      Assert.Equal("x", t.Labels[0]);
      Assert.Null(t.Values[0]);
    }
  }
}

