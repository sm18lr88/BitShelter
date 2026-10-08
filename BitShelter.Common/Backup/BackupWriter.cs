extern alias sc;

using sc::SharpCompress.Common;
using sc::SharpCompress.Common.Tar.Headers;
using sc::SharpCompress.Writers;
using sc::SharpCompress.Writers.Tar;
using sc::SharpCompress.Writers.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace BitShelter.Backup
{
  public static class BackupWriter
  {
    public static void WriteArchive(Stream output, IEnumerable<BackupFile> files, ArchiveType archiveType, CompressionType compressionType,
      BackupProgress progress, CancellationToken cancellationToken)
    {
      using (IWriter writer = OpenWriter(output, archiveType, compressionType))
      {
        foreach (BackupFile file in files)
        {
          cancellationToken.ThrowIfCancellationRequested();

          FileStream source;
          try
          {
            source = file.File.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
          }
          catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
          {
            progress.Skip(file.LogicalPath, ex);
            continue;
          }

          using (source)
            writer.Write(file.EntryName, source, file.File.LastWriteTime);

          progress.FileCount++;
          progress.InputBytes += file.File.Length;
        }
      }
    }

    // Copies the files into a folder tree. maxBytes is the limit for the total size (0 means no limit).
    public static void WriteFolder(string destination, IEnumerable<BackupFile> files, long maxBytes,
      BackupProgress progress, CancellationToken cancellationToken)
    {
      foreach (BackupFile file in files)
      {
        cancellationToken.ThrowIfCancellationRequested();

        if (maxBytes > 0 && progress.InputBytes + file.File.Length > maxBytes)
          throw new BackupSizeLimitException(maxBytes);

        string target = Path.Combine(destination, file.EntryName.Replace('/', Path.DirectorySeparatorChar));

        try
        {
          Directory.CreateDirectory(Path.GetDirectoryName(target));
          file.File.CopyTo(target, overwrite: false);
          File.SetLastWriteTimeUtc(target, file.File.LastWriteTimeUtc);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
          progress.Skip(file.LogicalPath, ex);
          continue;
        }

        progress.FileCount++;
        progress.InputBytes += file.File.Length;
      }
    }

    private static IWriter OpenWriter(Stream output, ArchiveType archiveType, CompressionType compressionType)
    {
      switch (archiveType)
      {
        case ArchiveType.Zip:
          return new ZipWriter(output, new ZipWriterOptions(compressionType, sc::SharpCompress.Compressors.Deflate.CompressionLevel.Default)
          {
            // Zip64 (archives over 4 GB) needs a seekable output. An encrypted output cannot seek.
            UseZip64 = output.CanSeek,
            LeaveStreamOpen = true,
          });

        case ArchiveType.Tar:
          return new TarWriter(output, new TarWriterOptions(compressionType, finalizeArchiveOnClose: true, TarHeaderWriteFormat.GNU_TAR_LONG_LINK)
          {
            LeaveStreamOpen = true,
          });

        default:
          throw new ArgumentException($"Archive type {archiveType} is not supported.", nameof(archiveType));
      }
    }
  }
}
