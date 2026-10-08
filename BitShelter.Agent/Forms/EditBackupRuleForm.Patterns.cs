using BitShelter.Models;
using DotNet.Globbing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BitShelter.Agent.Forms
{
  // Converts the include/exclude text boxes (one pattern per line) to path filters and back.
  partial class EditBackupRuleForm
  {
    private const string RegexPrefix = "regex:";

    internal static HashSet<PathFilter> ParsePatterns(IEnumerable<string> lines)
    {
      var filters = new HashSet<PathFilter>();

      foreach (string line in lines.Select(l => l.Trim()).Where(l => l.Length > 0))
      {
        bool regex = line.StartsWith(RegexPrefix, StringComparison.OrdinalIgnoreCase);
        var filter = new PathFilter
        {
          FilterPatternType = regex ? FilterPatternType.Regexp : FilterPatternType.Glob,
          Pattern = regex ? line.Substring(RegexPrefix.Length).Trim() : line,
        };

        try
        {
          if (regex)
            _ = new Regex(filter.Pattern);
          else
            Glob.Parse(filter.Pattern);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or InvalidOperationException)
        {
          throw new ArgumentException($"The pattern \"{line}\" is not valid: {ex.Message}");
        }

        filters.Add(filter);
      }

      return filters;
    }

    internal static string[] FormatPatterns(IEnumerable<PathFilter> filters)
    {
      return (filters ?? Enumerable.Empty<PathFilter>())
        .Select(f => f.FilterPatternType == FilterPatternType.Regexp ? RegexPrefix + f.Pattern : f.Pattern)
        .ToArray();
    }
  }
}
