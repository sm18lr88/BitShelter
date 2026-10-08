using BitShelter.Utils;
using System;
using Xunit;

namespace BitShelter.Tests.Utils
{
  public sealed class ReflectionTests
  {
    private sealed class Source
    {
      public int A { get; set; }
      public string? B { get; set; }
      public string ReadOnly => "x";
    }

    private sealed class Destination
    {
      public int A { get; set; }
      public string? B { get; private set; }
      public static string StaticProp { get; set; } = "";
    }

    [Fact]
    public void CopyProperties_copies_public_settable_properties_only()
    {
      var src = new Source { A = 12, B = "hello" };
      var dst = new Destination { A = 0 };

      src.CopyProperties(dst);

      Assert.Equal(12, dst.A);
      Assert.Null(dst.B);
    }

    [Fact]
    public void CopyProperties_null_arguments_throw()
    {
      Assert.Throws<Exception>(() => Reflection.CopyProperties(null!, new object()));
      Assert.Throws<Exception>(() => new object().CopyProperties(null!));
    }
  }
}

