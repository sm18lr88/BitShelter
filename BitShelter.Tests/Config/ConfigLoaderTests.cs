using BitShelter.Service.Config;

namespace BitShelter.Tests.Config
{
  public sealed class ConfigLoaderTests : IDisposable
  {
    private readonly string folder = Directory.CreateTempSubdirectory("bitshelter-tests-").FullName;

    public void Dispose() => Directory.Delete(folder, recursive: true);

    [Fact]
    public void WriteJsonAtomic_replaces_existing_file_and_leaves_no_temp_file()
    {
      string path = Path.Combine(folder, ConfigLoader.SnapshotInstancesFileName);
      File.WriteAllText(path, "old");

      ConfigLoader.WriteJsonAtomic(path, new[] { 1, 2, 3 });

      Assert.Equal("[1,2,3]", File.ReadAllText(path));
      Assert.Equal(new[] { path }, Directory.GetFiles(folder));
    }

    [Fact]
    public void DeleteOldRulesFiles_keeps_newest_rule_files_and_ignores_other_names()
    {
      foreach (long ticks in new long[] { 100, 300, 200, 500, 400 })
        File.WriteAllText(Path.Combine(folder, $"rule_{ticks}.json"), "[]");
      File.WriteAllText(Path.Combine(folder, "rule_1.json.bak"), "");
      File.WriteAllText(Path.Combine(folder, "myrule_2.json"), "");

      ConfigLoader.DeleteOldRulesFiles(folder, keepCount: 2);

      string[] remaining = Directory.GetFiles(folder).Select(Path.GetFileName).Order().ToArray()!;
      Assert.Equal(new[] { "myrule_2.json", "rule_1.json.bak", "rule_400.json", "rule_500.json" }, remaining);
    }

    [Fact]
    public void GetLatestRulesFileName_picks_highest_timestamp_not_lexical_order()
    {
      File.WriteAllText(Path.Combine(folder, "rule_99.json"), "[]");
      File.WriteAllText(Path.Combine(folder, "rule_100.json"), "[]");

      Assert.Equal("rule_100.json", Path.GetFileName(ConfigLoader.GetLatestRulesFileName(folder)));
    }
  }
}
