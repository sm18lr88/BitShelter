using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BitShelter.VSS
{
  // A failed VSS call. The message names the VSS error code, so logs say what went wrong.
  public sealed class VssException : COMException
  {
    public const int ObjectNotFound = unchecked((int)0x80042308);

    // VSS_E_* codes from vsserror.h (Windows SDK 10.0.28000).
    private static readonly Dictionary<int, string> Names = new Dictionary<int, string>
    {
      [unchecked((int)0x80042301)] = "VSS_E_BAD_STATE",
      [unchecked((int)0x80042302)] = "VSS_E_UNEXPECTED",
      [unchecked((int)0x80042303)] = "VSS_E_PROVIDER_ALREADY_REGISTERED",
      [unchecked((int)0x80042304)] = "VSS_E_PROVIDER_NOT_REGISTERED",
      [unchecked((int)0x80042306)] = "VSS_E_PROVIDER_VETO",
      [unchecked((int)0x80042307)] = "VSS_E_PROVIDER_IN_USE",
      [unchecked((int)0x80042308)] = "VSS_E_OBJECT_NOT_FOUND",
      [unchecked((int)0x8004230C)] = "VSS_E_VOLUME_NOT_SUPPORTED",
      [unchecked((int)0x8004230E)] = "VSS_E_VOLUME_NOT_SUPPORTED_BY_PROVIDER",
      [unchecked((int)0x8004230D)] = "VSS_E_OBJECT_ALREADY_EXISTS",
      [unchecked((int)0x8004230F)] = "VSS_E_UNEXPECTED_PROVIDER_ERROR",
      [unchecked((int)0x80042310)] = "VSS_E_CORRUPT_XML_DOCUMENT",
      [unchecked((int)0x80042311)] = "VSS_E_INVALID_XML_DOCUMENT",
      [unchecked((int)0x80042312)] = "VSS_E_MAXIMUM_NUMBER_OF_VOLUMES_REACHED",
      [unchecked((int)0x80042313)] = "VSS_E_FLUSH_WRITES_TIMEOUT",
      [unchecked((int)0x80042314)] = "VSS_E_HOLD_WRITES_TIMEOUT",
      [unchecked((int)0x80042315)] = "VSS_E_UNEXPECTED_WRITER_ERROR",
      [unchecked((int)0x80042316)] = "VSS_E_SNAPSHOT_SET_IN_PROGRESS",
      [unchecked((int)0x80042317)] = "VSS_E_MAXIMUM_NUMBER_OF_SNAPSHOTS_REACHED",
      [unchecked((int)0x80042318)] = "VSS_E_WRITER_INFRASTRUCTURE",
      [unchecked((int)0x80042319)] = "VSS_E_WRITER_NOT_RESPONDING",
      [unchecked((int)0x8004231A)] = "VSS_E_WRITER_ALREADY_SUBSCRIBED",
      [unchecked((int)0x8004231B)] = "VSS_E_UNSUPPORTED_CONTEXT",
      [unchecked((int)0x8004231D)] = "VSS_E_VOLUME_IN_USE",
      [unchecked((int)0x8004231E)] = "VSS_E_MAXIMUM_DIFFAREA_ASSOCIATIONS_REACHED",
      [unchecked((int)0x8004231F)] = "VSS_E_INSUFFICIENT_STORAGE",
      [unchecked((int)0x80042320)] = "VSS_E_NO_SNAPSHOTS_IMPORTED",
      [unchecked((int)0x80042321)] = "VSS_E_SOME_SNAPSHOTS_NOT_IMPORTED",
      [unchecked((int)0x80042322)] = "VSS_E_MAXIMUM_NUMBER_OF_REMOTE_MACHINES_REACHED",
      [unchecked((int)0x80042323)] = "VSS_E_REMOTE_SERVER_UNAVAILABLE",
      [unchecked((int)0x80042324)] = "VSS_E_REMOTE_SERVER_UNSUPPORTED",
      [unchecked((int)0x80042325)] = "VSS_E_REVERT_IN_PROGRESS",
      [unchecked((int)0x80042326)] = "VSS_E_REVERT_VOLUME_LOST",
      [unchecked((int)0x80042327)] = "VSS_E_REBOOT_REQUIRED",
      [unchecked((int)0x80042328)] = "VSS_E_TRANSACTION_FREEZE_TIMEOUT",
      [unchecked((int)0x80042329)] = "VSS_E_TRANSACTION_THAW_TIMEOUT",
      [unchecked((int)0x8004232D)] = "VSS_E_VOLUME_NOT_LOCAL",
      [unchecked((int)0x8004232E)] = "VSS_E_CLUSTER_TIMEOUT",
      [unchecked((int)0x800423F0)] = "VSS_E_WRITERERROR_INCONSISTENTSNAPSHOT",
      [unchecked((int)0x800423F1)] = "VSS_E_WRITERERROR_OUTOFRESOURCES",
      [unchecked((int)0x800423F2)] = "VSS_E_WRITERERROR_TIMEOUT",
      [unchecked((int)0x800423F3)] = "VSS_E_WRITERERROR_RETRYABLE",
      [unchecked((int)0x800423F4)] = "VSS_E_WRITERERROR_NONRETRYABLE",
      [unchecked((int)0x800423F5)] = "VSS_E_WRITERERROR_RECOVERY_FAILED",
      [unchecked((int)0x800423F6)] = "VSS_E_BREAK_REVERT_ID_FAILED",
      [unchecked((int)0x800423F7)] = "VSS_E_LEGACY_PROVIDER",
      [unchecked((int)0x800423F8)] = "VSS_E_MISSING_DISK",
      [unchecked((int)0x800423F9)] = "VSS_E_MISSING_HIDDEN_VOLUME",
      [unchecked((int)0x800423FA)] = "VSS_E_MISSING_VOLUME",
      [unchecked((int)0x800423FB)] = "VSS_E_AUTORECOVERY_FAILED",
      [unchecked((int)0x800423FC)] = "VSS_E_DYNAMIC_DISK_ERROR",
      [unchecked((int)0x800423FD)] = "VSS_E_NONTRANSPORTABLE_BCD",
      [unchecked((int)0x800423FE)] = "VSS_E_CANNOT_REVERT_DISKID",
      [unchecked((int)0x800423FF)] = "VSS_E_RESYNC_IN_PROGRESS",
      [unchecked((int)0x80042400)] = "VSS_E_CLUSTER_ERROR",
      [unchecked((int)0x8004232A)] = "VSS_E_UNSELECTED_VOLUME",
      [unchecked((int)0x8004232B)] = "VSS_E_SNAPSHOT_NOT_IN_SET",
      [unchecked((int)0x8004232C)] = "VSS_E_NESTED_VOLUME_LIMIT",
      [unchecked((int)0x8004232F)] = "VSS_E_NOT_SUPPORTED",
      [unchecked((int)0x80042336)] = "VSS_E_WRITERERROR_PARTIAL_FAILURE",
      [unchecked((int)0x80042401)] = "VSS_E_ASRERROR_DISK_ASSIGNMENT_FAILED",
      [unchecked((int)0x80042402)] = "VSS_E_ASRERROR_DISK_RECREATION_FAILED",
      [unchecked((int)0x80042403)] = "VSS_E_ASRERROR_NO_ARCPATH",
      [unchecked((int)0x80042404)] = "VSS_E_ASRERROR_MISSING_DYNDISK",
      [unchecked((int)0x80042405)] = "VSS_E_ASRERROR_SHARED_CRIDISK",
      [unchecked((int)0x80042406)] = "VSS_E_ASRERROR_DATADISK_RDISK0",
      [unchecked((int)0x80042407)] = "VSS_E_ASRERROR_RDISK0_TOOSMALL",
      [unchecked((int)0x80042408)] = "VSS_E_ASRERROR_CRITICAL_DISKS_TOO_SMALL",
      [unchecked((int)0x80042409)] = "VSS_E_WRITER_STATUS_NOT_AVAILABLE",
      [unchecked((int)0x8004240A)] = "VSS_E_ASRERROR_DYNAMIC_VHD_NOT_SUPPORTED",
      [unchecked((int)0x80042411)] = "VSS_E_CRITICAL_VOLUME_ON_INVALID_DISK",
      [unchecked((int)0x80042412)] = "VSS_E_ASRERROR_RDISK_FOR_SYSTEM_DISK_NOT_FOUND",
      [unchecked((int)0x80042413)] = "VSS_E_ASRERROR_NO_PHYSICAL_DISK_AVAILABLE",
      [unchecked((int)0x80042414)] = "VSS_E_ASRERROR_FIXED_PHYSICAL_DISK_AVAILABLE_AFTER_DISK_EXCLUSION",
      [unchecked((int)0x80042415)] = "VSS_E_ASRERROR_CRITICAL_DISK_CANNOT_BE_EXCLUDED",
      [unchecked((int)0x80042416)] = "VSS_E_ASRERROR_SYSTEM_PARTITION_HIDDEN",
      [unchecked((int)0x80042417)] = "VSS_E_FSS_TIMEOUT",
    };

    public VssException(string operation, int hresult)
      : base($"{operation} failed with {Describe(hresult)}.", hresult)
    {
    }

    public static string Describe(int hresult)
    {
      return Names.TryGetValue(hresult, out string name)
        ? $"{name} (0x{hresult:X8})"
        : $"0x{hresult:X8} ({Marshal.GetExceptionForHR(hresult)?.Message})";
    }

    internal static void ThrowIfFailed(int hresult, string operation)
    {
      if (hresult < 0)
        throw new VssException(operation, hresult);
    }
  }
}
