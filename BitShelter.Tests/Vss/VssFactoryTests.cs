using Alphaleonis.Win32.Vss;
using BitShelter.VSS;

namespace BitShelter.Tests.Vss
{
  public sealed class VssFactoryTests
  {
    // Loads AlphaVSS's C++/CLI platform assembly, which is what breaks first on a runtime or bitness change.
    // It does not touch the VSS service, so it needs no elevation.
    [Fact]
    public void AlphaVss_platform_assembly_loads_in_this_runtime()
    {
      Assert.True(Environment.Is64BitProcess);

      IVssFactory factory = new VssFactoryProvider(new VssAssemblyResolver()).GetVssFactory();

      Assert.NotNull(factory);
    }
  }
}
