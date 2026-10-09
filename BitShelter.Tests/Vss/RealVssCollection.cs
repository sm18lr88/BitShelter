namespace BitShelter.Tests.Vss
{
  // Tests that create real shadow copies. VSS takes one shadow copy set at a time, and it holds writes to the
  // volume while it creates one, so these tests run alone, after the parallel tests.
  [CollectionDefinition(nameof(RealVssCollection), DisableParallelization = true)]
  public sealed class RealVssCollection
  {
  }
}
