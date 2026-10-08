using BitShelter.Backup;
using BitShelter.Utils;
using BitShelter.VSS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BitShelter.Service.Backup
{
  // Maps each input folder of a backup rule to the same folder inside the new snapshot of its volume.
  internal static class SnapshotBackupSources
  {
    public static List<BackupSource> Create(IEnumerable<string> inputFolders, IReadOnlyList<VssSnapshotProperties> snapshots)
    {
      var sources = new List<BackupSource>();

      foreach (string inputFolder in inputFolders)
      {
        string fullPath = Path.GetFullPath(inputFolder);
        string volumeRoot = Volumes.GetVolumeRootPath(fullPath);
        string volumeName = Volumes.GetUniqueVolumeNameForVolumeMountPoint(volumeRoot);

        VssSnapshotProperties snapshot = snapshots.FirstOrDefault(s => string.Equals(s.OriginalVolumeName, volumeName, StringComparison.OrdinalIgnoreCase))
          ?? throw new InvalidOperationException($"The input folder {inputFolder} is not on a drive of this snapshot rule.");

        sources.Add(BackupSource.FromFolder(fullPath, MapToSnapshot(fullPath, volumeRoot, snapshot.SnapshotDeviceObject)));
      }

      return sources;
    }

    // Example: "D:\Docs" on volume "D:\" becomes "\?\GLOBALROOT\Device\HarddiskVolumeShadowCopy7\Docs".
    internal static string MapToSnapshot(string path, string volumeRoot, string snapshotDevice)
    {
      if (!path.StartsWith(volumeRoot, StringComparison.OrdinalIgnoreCase))
        throw new ArgumentException($"{path} is not on volume {volumeRoot}.", nameof(path));

      return snapshotDevice.TrimEnd('\\') + "\\" + path.Substring(volumeRoot.Length);
    }
  }
}
