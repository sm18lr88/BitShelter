extern alias sc;

using sc::SharpCompress.Common;
using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace BitShelter.Backup
{
  // Names of the backup folders and files. Retention finds earlier backups from these names,
  // so the output folder is the only record of which backups exist.
  public static class BackupNaming
  {
    public const string TimestampFormat = "yyyyMMdd-HHmmss";
    public const string PartialSuffix = ".partial";
    public const string EncryptedExtension = ".gpg";

    public static string GetBackupFolder(string outputFolder, string ruleName, string backupName)
    {
      return Path.Combine(outputFolder, Sanitize(ruleName) + " - " + Sanitize(backupName));
    }

    public static string GetEntryName(DateTime createdAt, string extension)
    {
      return createdAt.ToString(TimestampFormat, CultureInfo.InvariantCulture) + extension;
    }

    public static string GetExtension(bool archive, ArchiveType archiveType, CompressionType compressionType, bool encrypted)
    {
      if (!archive)
        return "";

      string extension = archiveType switch
      {
        ArchiveType.Zip => ".zip",
        ArchiveType.Tar => ".tar" + GetTarCompressionExtension(compressionType),
        _ => throw new ArgumentException($"Archive type {archiveType} is not supported.", nameof(archiveType)),
      };

      return encrypted ? extension + EncryptedExtension : extension;
    }

    // Returns false for names that do not start with a timestamp, and for unfinished backups.
    public static bool TryParseTimestamp(string entryName, out DateTime createdAt)
    {
      createdAt = default;

      if (entryName == null || entryName.Length < TimestampFormat.Length || entryName.EndsWith(PartialSuffix, StringComparison.OrdinalIgnoreCase))
        return false;

      return DateTime.TryParseExact(entryName.Substring(0, TimestampFormat.Length), TimestampFormat,
        CultureInfo.InvariantCulture, DateTimeStyles.None, out createdAt);
    }

    public static string Sanitize(string name)
    {
      char[] invalid = Path.GetInvalidFileNameChars();
      string clean = new string((name ?? "").Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim().TrimEnd('.');

      return clean.Length == 0 ? "_" : clean;
    }

    // "D:\Docs\Work" becomes "D/Docs/Work". Each input folder gets its own top-level entry in the backup.
    public static string GetSourceLabel(string inputFolder)
    {
      string trimmed = inputFolder.TrimEnd('\\', '/');

      return string.Join("/", trimmed.Replace(":", "").Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries).Select(Sanitize));
    }

    private static string GetTarCompressionExtension(CompressionType compressionType)
    {
      return compressionType switch
      {
        CompressionType.None => "",
        CompressionType.GZip => ".gz",
        CompressionType.BZip2 => ".bz2",
        CompressionType.LZip => ".lz",
        _ => throw new ArgumentException($"Compression {compressionType} is not supported for tar.", nameof(compressionType)),
      };
    }
  }
}
