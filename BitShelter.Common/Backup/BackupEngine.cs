using BitShelter.Encryption;
using BitShelter.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace BitShelter.Backup
{
  // Runs one backup rule: writes a new backup to "<output>\<rule> - <backup>\<timestamp><extension>",
  // then applies the count and total-size limits. A backup is written under a ".partial" name first,
  // and renamed only when it is complete.
  public static partial class BackupEngine
  {
    public static BackupResult Run(SnapshotRule rule, BackupRule backup, IReadOnlyList<BackupSource> sources, DateTime now,
      CancellationToken cancellationToken)
    {
      Validate(backup);

      string folder = BackupNaming.GetBackupFolder(backup.OutputFolder, rule.Name, backup.Name);
      bool encrypted = backup.Encryption != BackupEncryption.None;
      string finalPath = Path.Combine(folder, BackupNaming.GetEntryName(now,
        BackupNaming.GetExtension(backup.CompressionEnabled, backup.ArchiveType, backup.CompressionType, encrypted)));
      string partialPath = finalPath + BackupNaming.PartialSuffix;
      long maxBytes = Math.Max(0, backup.MaxSizeMB) * 1024L * 1024L;

      Directory.CreateDirectory(folder);
      DeletePartialEntries(folder);

      var progress = new BackupProgress();
      IEnumerable<BackupFile> files = sources.SelectMany(s =>
        BackupFileWalker.Enumerate(s, backup.FilterIncludes, backup.FilterExcludes, progress.Skip, cancellationToken));

      try
      {
        if (backup.CompressionEnabled)
        {
          WriteArchiveFile(partialPath, Path.GetFileName(finalPath), backup, files, maxBytes, progress, cancellationToken);
          File.Move(partialPath, finalPath);
        }
        else
        {
          BackupWriter.WriteFolder(partialPath, files, maxBytes, progress, cancellationToken);
          Directory.Move(partialPath, finalPath);
        }
      }
      catch
      {
        DeleteEntry(partialPath);
        throw;
      }

      ApplyRetention(rule, backup, finalPath);

      return new BackupResult
      {
        RuleId = rule.Id,
        RuleName = rule.Name,
        BackupName = backup.Name,
        StartedAt = now,
        FinishedAt = DateTime.Now,
        Success = true,
        OutputPath = finalPath,
        SizeBytes = GetEntrySize(finalPath),
        FileCount = progress.FileCount,
        SkippedFileCount = progress.SkippedCount,
        Message = progress.SkippedCount == 0
          ? $"{progress.FileCount} files backed up."
          : $"{progress.FileCount} files backed up. {progress.SkippedCount} files or folders could not be read (see the log).",
      };
    }

    public static void Validate(BackupRule backup)
    {
      if (string.IsNullOrWhiteSpace(backup.Name))
        throw new ArgumentException("The backup has no name.");

      if (string.IsNullOrWhiteSpace(backup.OutputFolder) || !Path.IsPathFullyQualified(backup.OutputFolder))
        throw new ArgumentException($"Backup \"{backup.Name}\": the output folder must be a full path.");

      if (backup.InputFolders == null || backup.InputFolders.Count == 0)
        throw new ArgumentException($"Backup \"{backup.Name}\" has no input folder.");

      if (backup.Encryption != BackupEncryption.None && !backup.CompressionEnabled)
        throw new ArgumentException($"Backup \"{backup.Name}\": encryption works only together with an archive.");

      if (backup.Encryption != BackupEncryption.None && backup.EncryptionType != EncryptionAlgorithm.None && !OpenPgpEncryption.IsSupported(backup.EncryptionType))
        throw new ArgumentException($"Backup \"{backup.Name}\": {backup.EncryptionType} is not an OpenPGP cipher.");
    }

    private static void WriteArchiveFile(string path, string messageName, BackupRule backup, IEnumerable<BackupFile> files, long maxBytes,
      BackupProgress progress, CancellationToken cancellationToken)
    {
      using (var fileStream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
      {
        using (var limited = new SizeLimitStream(fileStream, maxBytes))
        using (Stream encrypted = OpenEncryption(backup, limited, messageName))
          BackupWriter.WriteArchive(encrypted ?? limited, files, backup.ArchiveType, backup.CompressionType, progress, cancellationToken);

        fileStream.Flush(flushToDisk: true);
      }
    }

    private static Stream OpenEncryption(BackupRule backup, Stream output, string messageName)
    {
      // The literal packet carries the archive name without ".gpg", so "gpg --decrypt-files" restores it.
      string innerName = Path.GetFileNameWithoutExtension(messageName);

      switch (backup.Encryption)
      {
        case BackupEncryption.None:
          return null;

        case BackupEncryption.PgpPublicKey:
          return OpenPgpEncryption.OpenForPublicKey(output, backup.PgpPublicKey, backup.EncryptionType, innerName);

        case BackupEncryption.PgpPassphrase:
          return OpenPgpEncryption.OpenForPassphrase(output, PassphraseProtector.Unprotect(backup.ProtectedPassphrase), backup.EncryptionType, innerName);

        default:
          throw new ArgumentException($"Encryption {backup.Encryption} is not supported.");
      }
    }
  }
}
