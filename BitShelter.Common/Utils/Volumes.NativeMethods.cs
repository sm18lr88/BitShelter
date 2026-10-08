using System.Runtime.InteropServices;
using System.Text;

namespace BitShelter.Utils
{
  public static partial class Volumes
  {
    private static class NativeMethods
    {
      public const int MAX_PATH = 261;
      public const uint ERROR_INSUFFICIENT_BUFFER = 122;
      public const uint ERROR_MORE_DATA = 234;

      [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      public static extern bool GetVolumeNameForVolumeMountPointW([In] string lpszVolumeMountPoint, [Out] StringBuilder lpszVolumeName, uint cchBufferLength);

      [DllImport("ResUtils.dll", CharSet = CharSet.Unicode, SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      public static extern bool ClusterGetVolumeNameForVolumeMountPointW([In] string lpszVolumeMountPoint, [Out] StringBuilder lpszVolumeName, uint cchBufferLength);

      [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      public static extern bool GetVolumePathNameW([In] string lpszFileName, [Out] StringBuilder lpszVolumePathName, uint cchBufferLength);

      [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      public static extern bool GetVolumePathNamesForVolumeNameW(string lpszVolumeName, char[] lpszVolumePathNames, uint cchBuferLength, ref uint lpcchReturnLength);

      [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
      internal static extern uint QueryDosDeviceW(string lpDeviceName, [Out] char[] lpTargetPath, uint ucchMax);

      [DllImport("ResUtils.dll", CharSet = CharSet.Unicode, SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool ClusterIsPathOnSharedVolume(string lpszPathName);

      [DllImport("ResUtils.dll", CharSet = CharSet.Unicode, SetLastError = false, PreserveSig = true)]
      [return: MarshalAs(UnmanagedType.U4)]
      internal static extern int ClusterPrepareSharedVolumeForBackup(string lpszFileName, [Out] StringBuilder lpszVolumePathName, ref int lpcchVolumePathName, [Out] StringBuilder lpszVolumeName, ref int lpcchVolumeName);
    }
  }
}
