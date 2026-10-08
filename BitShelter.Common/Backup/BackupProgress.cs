using Serilog;
using System;

namespace BitShelter.Backup
{
  public sealed class BackupProgress
  {
    public int FileCount { get; set; }
    public long InputBytes { get; set; }
    public int SkippedCount { get; private set; }

    // A file or folder that cannot be read is skipped and logged. The backup continues.
    public void Skip(string path, Exception ex)
    {
      SkippedCount++;
      Log.Warning(ex, "Backup skipped {Path}", path);
    }
  }

  public sealed class BackupSizeLimitException : InvalidOperationException
  {
    public BackupSizeLimitException(long maxBytes)
      : base($"The backup is larger than its size limit of {maxBytes / (1024 * 1024)} MB.") { }
  }
}
