using BitShelter.VSS;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace BitShelter.Utils
{
  public static partial class Volumes
  {
    // Returns the root of the volume that contains the path, for example "D:\" or a mount folder.
    public static string GetVolumeRootPath(string path)
    {
      StringBuilder volumeRootPath = new StringBuilder(NativeMethods.MAX_PATH);

      if (!NativeMethods.GetVolumePathNameW(path, volumeRootPath, (uint)volumeRootPath.Capacity))
        throw new Win32Exception();

      return volumeRootPath.ToString();
    }

    public static bool IsVolume(IUIHost host, string volumePath)
    {

      if (volumePath == null)
        throw new ArgumentNullException("volumePath");

      if (volumePath.Length == 0)
        return false;

      host.WriteVerbose("- Checking if \"{0}\" is a real volume path...", volumePath);

      if (!volumePath.EndsWith("\\"))
        volumePath = volumePath + "\\";

      StringBuilder volumeNameBuilder = new StringBuilder(NativeMethods.MAX_PATH);
      if (ClusterIsPathOnSharedVolume(host, volumePath))
      {
        if (!NativeMethods.ClusterGetVolumeNameForVolumeMountPointW(volumePath, volumeNameBuilder, (uint)volumeNameBuilder.Capacity))
        {
          host.WriteVerbose("- ClusterGetVolumeNameForVolumeMountPointW(\"{0}\") failed with error code {1}.", volumePath, Marshal.GetLastWin32Error());
          return false;
        }
        return true;
      }
      else
      {
        if (!NativeMethods.GetVolumeNameForVolumeMountPointW(volumePath, volumeNameBuilder, (uint)volumeNameBuilder.Capacity))
        {
          host.WriteVerbose("- GetVolumeNameForVolumeMountPoint(\"{0}\") failed with error code {1}.", volumePath, Marshal.GetLastWin32Error());
          return false;
        }
        return true;
      }

    }

    public static string GetUniqueVolumeNameForPath(IUIHost host, string path, bool isBackup)
    {
      if (path == null)
        throw new ArgumentNullException("path");

      if (path.Length == 0)
        throw new ArgumentException("Mount point must be non-empty");

      host.WriteVerbose("- Get volume path name for \"{0}\"...", path);

      if (!path.EndsWith("\\"))
        path = path + "\\";

      if (isBackup && ClusterIsPathOnSharedVolume(host, path))
      {
        string volumeRootPath, volumeUniqueName;
        ClusterPrepareSharedVolumeForBackup(path, out volumeRootPath, out volumeUniqueName);
        host.WriteVerbose("- Path name: {0}", volumeRootPath);
        host.WriteVerbose("- Unique volume name: {0}", volumeUniqueName);
        return volumeUniqueName;
      }
      else
      {
        // Get the root path of the volume
        StringBuilder volumeRootPath = new StringBuilder(NativeMethods.MAX_PATH);
        if (!NativeMethods.GetVolumePathNameW(path, volumeRootPath, (uint)volumeRootPath.Capacity))
        {
          host.WriteVerbose("- GetVolumePathName(\"{0}\") failed with error code {1}", path, Marshal.GetLastWin32Error());
          throw new Win32Exception();
        }

        // Get the volume name alias (might be different from the unique volume name in rare cases)
        StringBuilder volumeName = new StringBuilder(NativeMethods.MAX_PATH);
        if (!NativeMethods.GetVolumeNameForVolumeMountPointW(volumeRootPath.ToString(), volumeName, (uint)volumeName.Capacity))
        {
          host.WriteVerbose("- GetVolumeNameForVolumeMountPoint(\"{0}\") failed with error code {1}", volumeRootPath.ToString(), Marshal.GetLastWin32Error());
          throw new Win32Exception();
        }

        // Gte the unique volume name
        StringBuilder uniqueVolumeName = new StringBuilder(NativeMethods.MAX_PATH);
        if (!NativeMethods.GetVolumeNameForVolumeMountPointW(volumeName.ToString(), uniqueVolumeName, (uint)uniqueVolumeName.Capacity))
        {
          host.WriteVerbose("- GetVolumeNameForVolumeMountPoint(\"{0}\") failed with error code {1}", volumeName.ToString(), Marshal.GetLastWin32Error());
          throw new Win32Exception();
        }

        return uniqueVolumeName.ToString();
      }
    }

    /// <summary>
    ///  Retreives the Win32 device name from the volume name
    /// </summary>
    /// <param name="volumeName">Name of the volume. A trailing backslash is not allowed.</param>
    /// <returns>The Win32 device name from the volume name</returns>
    public static string GetDeviceForVolumeName(string volumeName)
    {
      if (volumeName == null)
        throw new ArgumentNullException("volumeName");

      if (volumeName.Length == 0)
        throw new ArgumentException("Volume name must be non-empty");

      // Eliminate the GLOBALROOT prefix if present
      const string globalRootPrefix = @"\\?\GLOBALROOT";

      if (volumeName.StartsWith(globalRootPrefix, StringComparison.OrdinalIgnoreCase))
        return volumeName.Substring(globalRootPrefix.Length);

      // If this is a volume name, get the device
      const string dosPrefix = @"\\?\";
      const string volumePrefix = @"\\?\Volume";

      if (volumeName.StartsWith(volumePrefix, StringComparison.OrdinalIgnoreCase))
      {
        // Isolate the DOS device for the volume name (in the format Volume{GUID})
        string dosDevice = volumeName.Substring(dosPrefix.Length);

        // Get the real device underneath
        return QueryDosDevice(dosDevice)[0];
      }

      return volumeName;
    }
  }
}
