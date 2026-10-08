using Alphaleonis.Win32.Vss;
using BitShelter.Models;
using BitShelter.VSS;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BitShelter.Utils
{
  /// BitShelter - Note: Methods implemented here seem to be missing from AlphaFS
  /// Based on AlphaFS
  /// 
  /// <summary>
  /// This class is a slimmed down version of the Volume-class in AlphaFS (http://alphafs.codeplex.com). It is included
  /// in this sample to avoid having a dependency on AlphaFS in here, but please use the AlphaFS version for any 
  /// production code.
  /// </summary>
  public static partial class Volumes
  {
    public static List<Volume> ListVolumes()
    {
      List<Volume> volumes = new List<Volume>();

      ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Volume");
      ManagementObjectCollection collection = searcher.Get();

      foreach (ManagementObject item in collection)
        volumes.Add(new Volume()
        {
          Name = (string)item["Name"],
          Label = (string)item["Label"],
          MountLetter = (string)item["DriveLetter"],
          DeviceID = (string)item["DeviceID"]
        });

      return volumes;
    }

    public static IEnumerable<Volume> GetVolumesForDevicesID(IEnumerable<string> devicesID)
    {
      return ListVolumes().Where(v => devicesID.Contains(v.DeviceID));
    }

    /// <summary>
    /// Retrieves information about MS-DOS device names. 
    /// The function can obtain the current mapping for a particular MS-DOS device name. 
    /// The function can also obtain a list of all existing MS-DOS device names.
    /// </summary>
    /// <param name="device">The device.</param>
    /// <returns>An MS-DOS device name string specifying the target of the query. The device name cannot have a 
    /// trailing backslash. This parameter can be <see langword="null"/>. In that case, the QueryDosDevice function 
    /// will return an array of all existing MS-DOS device names</returns>
    /// <remarks>See documentation on MSDN for the Windows QueryDosDevice() method for more information.</remarks>
    public static string[] QueryDosDevice(string device)
    {
      uint returnSize = 0;
      int maxSize = 260;

      List<string> l = new List<string>();

      while (true)
      {
        char[] buffer = new char[maxSize];

        returnSize = NativeMethods.QueryDosDeviceW(device, buffer, (uint)buffer.Length);
        int lastError = Marshal.GetLastWin32Error();

        if (lastError == 0 && returnSize > 0)
        {
          StringBuilder sb = new StringBuilder();

          for (int i = 0; i < returnSize; i++)
          {
            if (buffer[i] != '\0')
              sb.Append(buffer[i]);
            else if (sb.Length > 0)
            {
              l.Add(sb.ToString());
              sb.Length = 0;
            }
          }

          return l.ToArray();
        }
        else if (lastError == NativeMethods.ERROR_INSUFFICIENT_BUFFER)
        {
          maxSize *= 2;
        }
        else
        {
          throw new Win32Exception(lastError);
        }
      }
    }

    /// <summary>
    /// Gets the shortest display name for the specified <paramref name="volumeName"/>.
    /// </summary>
    /// <param name="volumeName">The volume name.</param>
    /// <returns>The shortest display name for the specified volume found, or <see langword="null"/> if no display names were found.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="volumeName"/> is a <see langword="null"/> reference</exception>
    /// <exception cref="Win32Exception">An error occured during a system call, such as the volume name specified was invalid or did not exist.</exception>
    /// <remarks>This method basically returns the shortest string returned by <see cref="GetVolumePathNamesForVolume"/></remarks>
    public static string GetDisplayNameForVolume(string volumeName)
    {
      string[] volumeMountPoints = GetVolumePathNamesForVolume(volumeName);

      if (volumeMountPoints.Length == 0)
        return null;

      string smallestMountPoint = volumeMountPoints[0];
      for (int i = 1; i < volumeMountPoints.Length; i++)
      {
        if (volumeMountPoints[i].Length < smallestMountPoint.Length)
          smallestMountPoint = volumeMountPoints[i];
      }
      return smallestMountPoint;
    }

    /// <summary>
    /// Retrieves a list of path names for the specified volume name.
    /// </summary>
    /// <param name="volumeName">The volume name.</param>
    /// <returns>An array containing the path names for the specified volume.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="volumeName"/> is a <see langword="null"/> reference</exception>
    /// <exception cref="System.IO.FileNotFoundException">The volume name specified was invalid, did not exist or was not ready.</exception>
    /// <remarks>For more information about this method see the MSDN documentation on GetVolumePathNamesForVolumeName().</remarks>
    public static string[] GetVolumePathNamesForVolume(string volumeName)
    {
      if (volumeName == null)
        throw new ArgumentNullException("volumeName");

      uint requiredLength = 0;
      char[] buffer = new char[NativeMethods.MAX_PATH];

      if (!NativeMethods.GetVolumePathNamesForVolumeNameW(volumeName, buffer, (uint)buffer.Length, ref requiredLength))
      {
        int errorCode = Marshal.GetLastWin32Error();
        if (errorCode == NativeMethods.ERROR_MORE_DATA || errorCode == NativeMethods.ERROR_INSUFFICIENT_BUFFER)
        {
          buffer = new char[requiredLength];
          if (!NativeMethods.GetVolumePathNamesForVolumeNameW(volumeName, buffer, (uint)buffer.Length, ref requiredLength))
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }
        else
        {
          throw new Win32Exception();
        }
      }

      List<string> displayNames = new List<string>();
      StringBuilder displayName = new StringBuilder();

      for (int i = 0; i < requiredLength; i++)
      {
        if (buffer[i] == '\0')
        {
          if (displayName.Length > 0)
            displayNames.Add(displayName.ToString());
          displayName.Length = 0;
        }
        else
        {
          displayName.Append(buffer[i]);
        }
      }

      return displayNames.ToArray();
    }

    /// <summary>
    /// Retrieves the unique volume name for the specified volume mount point or root directory.
    /// </summary>
    /// <param name="mountPoint">The path of a volume mount point (with or without a trailing backslash, "\") or a drive letter indicating a root directory (eg. "C:" or "D:\"). A trailing backslash is required.</param>
    /// <returns>The unique volume name of the form "\\?\Volume{GUID}\" where GUID is the GUID that identifies the volume.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="mountPoint"/> is a <see langword="null"/> reference</exception>
    /// <exception cref="ArgumentException"><paramref name="mountPoint"/> is an empty string</exception>        
    /// <exception cref="Win32Exception">Upon error retreiving the volume name</exception>
    /// <remarks>See the MSDN documentation on the method GetVolumeNameForVolumeMountPoint() for more information.</remarks>
    public static string GetUniqueVolumeNameForVolumeMountPoint(string mountPoint)
    {
      if (mountPoint == null)
        throw new ArgumentNullException("mountPoint");

      if (mountPoint.Length == 0)
        throw new ArgumentException("Mount point must be non-empty");

      // Get the volume name alias. This may be different from the unique volume name in some
      // rare cases.
      StringBuilder volumeName = new StringBuilder(NativeMethods.MAX_PATH);
      if (!NativeMethods.GetVolumeNameForVolumeMountPointW(mountPoint, volumeName, (uint)volumeName.Capacity))
        throw new Win32Exception();

      // Get the unique volume name
      StringBuilder uniqueVolumeName = new StringBuilder(NativeMethods.MAX_PATH);
      if (!NativeMethods.GetVolumeNameForVolumeMountPointW(volumeName.ToString(), uniqueVolumeName, (uint)volumeName.Capacity))
        throw new Win32Exception();

      return uniqueVolumeName.ToString();
    }
  }
}
