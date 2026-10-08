using BitShelter.VSS;
using System;
using System.ComponentModel;
using System.Text;

namespace BitShelter.Utils
{
  public static partial class Volumes
  {
    public static bool ClusterIsPathOnSharedVolume(IUIHost host, string path)
    {
      host.WriteVerbose("- Calling ClusterIsPathOnSharedVolume(\"{0}\")...", path);
      try
      {
        return NativeMethods.ClusterIsPathOnSharedVolume(path);
      }
      catch (EntryPointNotFoundException)
      {
        host.WriteVerbose("- ClusterIsPathOnSharedVolume is not available on this system.");
        return false;
      }
    }

    public static string ClusterGetVolumeNameForVolumeMountPoint(IUIHost host, string volumeMountPoint)
    {
      host.WriteVerbose("- Calling ClusterGetVolumeNameForVolumeMountPoint(\"{0}\")...", volumeMountPoint);
      StringBuilder result = new StringBuilder(NativeMethods.MAX_PATH);
      if (!NativeMethods.ClusterGetVolumeNameForVolumeMountPointW(volumeMountPoint, result, (uint)result.Capacity))
        throw new Win32Exception();
      return result.ToString();
    }

    private static void ClusterPrepareSharedVolumeForBackup(string fileName, out string volumePathName, out string volumeName)
    {
      StringBuilder volumeRootPath = new StringBuilder(NativeMethods.MAX_PATH);
      StringBuilder volumeUniqueName = new StringBuilder(NativeMethods.MAX_PATH);
      int volumeRootPathCount = volumeRootPath.Capacity;
      int volumeUniqueNameCount = volumeUniqueName.Capacity;
      int ret = NativeMethods.ClusterPrepareSharedVolumeForBackup(fileName, volumeRootPath, ref volumeRootPathCount, volumeUniqueName, ref volumeUniqueNameCount);
      if (ret != 0)
        throw new Win32Exception(ret);

      volumePathName = volumeRootPath.ToString();
      volumeName = volumeUniqueName.ToString();
    }
  }
}
