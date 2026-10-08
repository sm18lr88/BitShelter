using System;
using System.Collections.Generic;
using System.Linq;

namespace BitShelter.Data
{
  public sealed class ExistingSnapshot
  {
    public Guid SnapshotId { get; set; }
    public string VolumeName { get; set; }
  }

  // Global pruning strategy: selects the snapshots of one rule to delete, so that each volume of the rule
  // has room for one new snapshot under the limit. Only snapshots of this rule are selected, oldest first.
  public static class VolumeLimitPruning
  {
    public static IReadOnlyList<Guid> SelectToMakeRoom(
      IEnumerable<ExistingSnapshot> existing,
      IEnumerable<(Guid SnapshotId, DateTime CreatedAt)> ruleSnapshots,
      IEnumerable<string> ruleVolumes,
      int limit,
      ISet<Guid> excluded,
      out IReadOnlyList<string> volumesStillFull)
    {
      List<ExistingSnapshot> all = existing.ToList();
      Dictionary<Guid, DateTime> owned = ruleSnapshots.ToDictionary(s => s.SnapshotId, s => s.CreatedAt);
      var selected = new List<Guid>();
      var stillFull = new List<string>();

      foreach (string volume in ruleVolumes.Distinct(StringComparer.OrdinalIgnoreCase))
      {
        List<ExistingSnapshot> onVolume = all.Where(s => string.Equals(s.VolumeName, volume, StringComparison.OrdinalIgnoreCase)).ToList();
        int needed = onVolume.Count - Math.Max(1, limit) + 1;

        if (needed <= 0)
          continue;

        List<Guid> candidates = onVolume.Where(s => owned.ContainsKey(s.SnapshotId) && excluded?.Contains(s.SnapshotId) != true)
                                        .OrderBy(s => owned[s.SnapshotId])
                                        .Select(s => s.SnapshotId)
                                        .Take(needed)
                                        .ToList();

        selected.AddRange(candidates);

        if (candidates.Count < needed)
          stillFull.Add(volume);
      }

      volumesStillFull = stillFull;
      return selected;
    }
  }
}
