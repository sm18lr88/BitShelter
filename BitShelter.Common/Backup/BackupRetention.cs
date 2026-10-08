using System;
using System.Collections.Generic;
using System.Linq;

namespace BitShelter.Backup
{
  public sealed class BackupEntry
  {
    public string Path { get; set; }
    public DateTime CreatedAt { get; set; }
    public long SizeBytes { get; set; }
  }

  // Selects the backups to delete. The backup that was just created is never selected.
  public static class BackupRetention
  {
    // Keeps the newest maxCount backups of one backup rule. 0 means no limit.
    public static IReadOnlyList<BackupEntry> SelectOverCount(IEnumerable<BackupEntry> entries, int maxCount, string keepPath)
    {
      if (maxCount <= 0)
        return Array.Empty<BackupEntry>();

      return entries.OrderByDescending(e => e.CreatedAt)
                    .Skip(maxCount)
                    .Where(e => !IsSamePath(e.Path, keepPath))
                    .ToList();
    }

    // Deletes the oldest backups of a snapshot rule until the total size is at most maxTotalBytes. 0 means no limit.
    public static IReadOnlyList<BackupEntry> SelectOverTotalSize(IEnumerable<BackupEntry> entries, long maxTotalBytes, string keepPath)
    {
      var selected = new List<BackupEntry>();

      if (maxTotalBytes <= 0)
        return selected;

      List<BackupEntry> all = entries.ToList();
      long total = all.Sum(e => e.SizeBytes);

      foreach (BackupEntry entry in all.OrderBy(e => e.CreatedAt))
      {
        if (total <= maxTotalBytes)
          break;

        if (IsSamePath(entry.Path, keepPath))
          continue;

        selected.Add(entry);
        total -= entry.SizeBytes;
      }

      return selected;
    }

    private static bool IsSamePath(string a, string b)
    {
      return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }
  }
}
