using BitShelter.Filters;
using BitShelter.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace BitShelter.Backup
{
  public static class BackupFileWalker
  {
    // An empty include list includes every file. An exclude match on a folder skips the whole folder.
    // Junctions and symbolic links (to folders or files) are not followed, so a link cannot pull in data outside
    // the input folder. An absolute link target would also resolve to the live volume, not to the snapshot.
    public static IEnumerable<BackupFile> Enumerate(
      BackupSource source,
      ICollection<PathFilter> includes,
      ICollection<PathFilter> excludes,
      Action<string, Exception> onError,
      CancellationToken cancellationToken)
    {
      includes = includes ?? Array.Empty<PathFilter>();
      excludes = excludes ?? Array.Empty<PathFilter>();

      var pending = new Stack<string>();
      pending.Push("");

      while (pending.Count > 0)
      {
        cancellationToken.ThrowIfCancellationRequested();

        string relativeDir = pending.Pop();
        FileSystemInfo[] items;

        try
        {
          items = new DirectoryInfo(Join(source.ReadFolder, relativeDir)).GetFileSystemInfos();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
          onError?.Invoke(Join(source.InputFolder, relativeDir), ex);
          continue;
        }

        foreach (FileSystemInfo item in items.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase))
        {
          string relative = relativeDir.Length == 0 ? item.Name : relativeDir + "\\" + item.Name;
          string logicalPath = Join(source.InputFolder, relative);

          if (excludes.Any(f => f.Apply(logicalPath)))
            continue;

          if (item is DirectoryInfo)
          {
            if (!item.Attributes.HasFlag(FileAttributes.ReparsePoint))
              pending.Push(relative);

            continue;
          }

          if (item.LinkTarget != null)
            continue;

          if (includes.Count > 0 && !includes.Any(f => f.Apply(logicalPath)))
            continue;

          yield return new BackupFile
          {
            File = (FileInfo)item,
            LogicalPath = logicalPath,
            EntryName = source.Label + "/" + relative.Replace('\\', '/'),
          };
        }
      }
    }

    private static string Join(string root, string relative)
    {
      return relative.Length == 0 ? root : root.TrimEnd('\\') + "\\" + relative;
    }
  }
}
