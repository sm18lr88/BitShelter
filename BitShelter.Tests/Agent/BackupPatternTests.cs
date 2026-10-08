using BitShelter.Agent.Forms;
using BitShelter.Models;

namespace BitShelter.Tests.Agent
{
  public sealed class BackupPatternTests
  {
    [Fact]
    public void Patterns_roundtrip_with_glob_default_and_regex_prefix()
    {
      HashSet<PathFilter> filters = EditBackupRuleForm.ParsePatterns(new[] { "**/*.docx", "", "  regex:\\.tmp$  " });

      Assert.Contains(filters, f => f.FilterPatternType == FilterPatternType.Glob && f.Pattern == "**/*.docx");
      Assert.Contains(filters, f => f.FilterPatternType == FilterPatternType.Regexp && f.Pattern == "\\.tmp$");
      Assert.Equal(new[] { "**/*.docx", "regex:\\.tmp$" }, EditBackupRuleForm.FormatPatterns(filters).Order());
    }

    [Fact]
    public void Invalid_regex_is_rejected()
    {
      Assert.Throws<ArgumentException>(() => EditBackupRuleForm.ParsePatterns(new[] { "regex:(" }));
    }
  }
}
