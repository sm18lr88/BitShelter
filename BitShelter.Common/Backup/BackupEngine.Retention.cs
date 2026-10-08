using BitShelter.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BitShelter.Backup
{
  public static partial class BackupEngine
  {
    private static void ApplyRetention(SnapshotRule rule, BackupRule backup, string newBackupPath)
    {
      string folder = Path.GetDirectoryName(newBackupPath);

      foreach (BackupEntry entry in BackupRetention.SelectOverCount(ListEntries(folder), backup.MaxBackupCount, newBackupPath))
        DeleteOldBackup(entry);

      long maxTotalBytes = Math.Max(0, rule.BackupTotalMaxSize) * (long)rule.BackupTotalMaxSizeUnit;
      if (maxTotalBytes <= 0)
        return;

      IEnumerable<BackupEntry> allEntries = rule.BackupRules
        .Where(b => !string.IsNullOrWhiteSpace(b.OutputFolder) && !string.IsNullOrWhiteSpace(b.Name))
        .Select(b => BackupNaming.GetBackupFolder(b.OutputFolder, rule.Name, b.Name))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .SelectMany(ListEntries);

      foreach (BackupEntry entry in BackupRetention.SelectOverTotalSize(allEntries, maxTotalBytes, newBackupPath))
        DeleteOldBackup(entry);
    }

    public static IReadOnlyList<BackupEntry> ListEntries(string folder)
    {
      if (!Directory.Exists(folder))
        return Array.Empty<BackupEntry>();

      var entries = new List<BackupEntry>();

      foreach (string path in Directory.EnumerateFileSystemEntries(folder))
        if (BackupNaming.TryParseTimestamp(Path.GetFileName(path), out DateTime createdAt))
          entries.Add(new BackupEntry { Path = path, CreatedAt = createdAt, SizeBytes = GetEntrySize(path) });

      return entries;
    }

    private static long GetEntrySize(string path)
    {
      if (File.Exists(path))
        return new FileInfo(path).Length;

      return Directory.Exists(path)
        ? new DirectoryInfo(path).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length)
        : 0;
    }

    // Only one backup runs at a time, so a ".partial" entry is left over from a backup that was interrupted.
    private static void DeletePartialEntries(string folder)
    {
      foreach (string path in Directory.EnumerateFileSystemEntries(folder, "*" + BackupNaming.PartialSuffix))
        DeleteEntry(path);
    }

    private static void DeleteOldBackup(BackupEntry entry)
    {
      Log.Information("Deleting old backup {Path}", entry.Path);
      DeleteEntry(entry.Path);
    }

    private static void DeleteEntry(string path)
    {
      try
      {
        if (File.Exists(path))
          File.Delete(path);
        else if (Directory.Exists(path))
          Directory.Delete(path, recursive: true);
      }
      catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
      {
        Log.Warning(ex, "Could not delete {Path}", path);
      }
    }
  }
}
