using System;

namespace BitShelter.Backup
{
  // Decides which snapshots of a rule also produce a backup.
  public static class BackupSchedule
  {
    // Snapshot numbers start at 0 when backups are first enabled for a rule.
    // A backup rule runs on snapshot number Offset, Offset + Every, Offset + 2 * Every, and so on.
    public static bool IsDue(long snapshotNumber, int offset, int every)
    {
      long first = Math.Max(0, offset);
      long step = Math.Max(1, every);

      return snapshotNumber >= first && (snapshotNumber - first) % step == 0;
    }
  }
}
