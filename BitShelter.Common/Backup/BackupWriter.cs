extern alias sc;

using sc::SharpCompress.Common;
using sc::SharpCompress.Common.Tar.Headers;
using sc::SharpCompress.Writers;
using sc::SharpCompress.Writers.Tar;
using sc::SharpCompress.Writers.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace BitShelter.Backup
{
  public static class BackupWriter
  {
    public static void WriteArchive(Stream output, IEnumerable<BackupFile> files, ArchiveType archiveType, CompressionType compressionType,
      BackupProgress progress, CancellationToken cancellationToken)
    {
      if (archiveType == ArchiveType.Zip && !output.CanSeek)
      {
        WriteStreamedZip(output, files, compressionType, progress, cancellationToken);
        return;
      }

      using (IWriter writer = OpenWriter(output, archiveType, compressionType))
        WriteFiles(files, progress, cancellationToken, (file, source) => writer.Write(file.EntryName, source, file.File.LastWriteTime));
    }

    // An encrypted backup cannot seek. SharpCompress cannot write Zip64 (archives over 4 GB) to such an output,
    // but System.IO.Compression can: it writes data descriptors with 64-bit sizes. It has only the Deflate and
    // None methods. The Agent offers only these two for an encrypted zip; an older rule with BZip2 or PPMd
    // gets Deflate.
    private static void WriteStreamedZip(Stream output, IEnumerable<BackupFile> files, CompressionType compressionType,
      BackupProgress progress, CancellationToken cancellationToken)
    {
      CompressionLevel level = compressionType == CompressionType.None ? CompressionLevel.NoCompression : CompressionLevel.Optimal;

      using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
      {
        WriteFiles(files, progress, cancellationToken, (file, source) =>
        {
          ZipArchiveEntry entry = zip.CreateEntry(file.EntryName, level);

          // A zip entry stores times from 1980 to 2107 only.
          DateTime modified = file.File.LastWriteTime;
          if (modified.Year >= 1980 && modified.Year <= 2107)
            entry.LastWriteTime = modified;

          using (Stream target = entry.Open())
            source.CopyTo(target);
        });
      }
    }

    private static void WriteFiles(IEnumerable<BackupFile> files, BackupProgress progress, CancellationToken cancellationToken,
      Action<BackupFile, Stream> write)
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
          write(file, source);

        progress.FileCount++;
        progress.InputBytes += file.File.Length;
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
            // Only a seekable output gets here (see WriteArchive), so Zip64 (archives over 4 GB) always works.
            UseZip64 = true,
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
