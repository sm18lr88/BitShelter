using BitShelter.Models;
using Xunit;

namespace BitShelter.Tests.Models
{
  public sealed class PathFilterTests
  {
    [Fact]
    public void Equals_and_GetHashCode_handle_null_pattern()
    {
      var a = new PathFilter { Pattern = null };
      var b = new PathFilter { Pattern = null };
      var c = new PathFilter { Pattern = "x" };

      Assert.True(a.Equals(b));
      Assert.False(a.Equals(c));
      Assert.Equal(0, a.GetHashCode());
    }
  }
}

