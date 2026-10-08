using BitShelter.Data;

namespace BitShelter.Tests.Data
{
  public sealed class VolumeLimitPruningTests
  {
    private static readonly Guid A1 = Guid.NewGuid(), A2 = Guid.NewGuid(), A3 = Guid.NewGuid(), Other = Guid.NewGuid(), OnE = Guid.NewGuid();

    private static ExistingSnapshot[] Existing => new[]
    {
      new ExistingSnapshot { SnapshotId = Other, VolumeName = "D" },
      new ExistingSnapshot { SnapshotId = A1, VolumeName = "D" },
      new ExistingSnapshot { SnapshotId = A2, VolumeName = "D" },
      new ExistingSnapshot { SnapshotId = A3, VolumeName = "D" },
      new ExistingSnapshot { SnapshotId = OnE, VolumeName = "E" },
    };

    private static (Guid, DateTime)[] Owned => new[] { (A3, new DateTime(2026, 1, 3)), (A1, new DateTime(2026, 1, 1)), (A2, new DateTime(2026, 1, 2)) };

    [Fact]
    public void Deletes_the_oldest_snapshots_of_the_rule_to_leave_room_for_one()
    {
      IReadOnlyList<Guid> selected = VolumeLimitPruning.SelectToMakeRoom(Existing, Owned, new[] { "D", "E" }, limit: 3, new HashSet<Guid>(), out var stillFull);

      Assert.Equal(new[] { A1, A2 }, selected);
      Assert.Empty(stillFull);
    }

    [Fact]
    public void Never_selects_snapshots_of_other_rules_or_excluded_ones()
    {
      IReadOnlyList<Guid> selected = VolumeLimitPruning.SelectToMakeRoom(Existing, Owned, new[] { "D" }, limit: 1, new HashSet<Guid> { A2 }, out var stillFull);

      Assert.Equal(new[] { A1, A3 }, selected);
      Assert.Equal(new[] { "D" }, stillFull);
    }

    [Fact]
    public void Selects_nothing_below_the_limit()
    {
      Assert.Empty(VolumeLimitPruning.SelectToMakeRoom(Existing, Owned, new[] { "D" }, limit: 64, new HashSet<Guid>(), out _));
    }
  }
}
