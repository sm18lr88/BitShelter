using BitShelter.Service.Jobs;

namespace BitShelter.Tests.Data
{
  public sealed class BackupQueueTests
  {
    private readonly Dictionary<Guid, int> pins = new();
    private readonly BackupQueue queue;

    public BackupQueueTests()
    {
      queue = new BackupQueue(ids => Change(ids, +1), ids => Change(ids, -1));
    }

    [Fact]
    public void A_new_snapshot_replaces_the_waiting_backup_of_its_rule()
    {
      Guid first = Guid.NewGuid(), second = Guid.NewGuid();

      Assert.True(queue.Enqueue(1, new[] { first }, new[] { "Docs" }));
      Assert.False(queue.Enqueue(1, new[] { second }, new[] { "Photos" }));

      Assert.Equal(0, pins.GetValueOrDefault(first));
      Assert.Equal(1, pins.GetValueOrDefault(second));

      BackupQueue.Entry entry = queue.Take(1)!;
      Assert.Equal(new[] { second }, entry.SnapshotIds);
      Assert.Equal(new HashSet<string> { "Docs", "Photos" }, entry.BackupNames);
      Assert.Null(queue.Take(1));

      queue.Finish(entry);
      Assert.Equal(0, pins.GetValueOrDefault(second));
    }

    [Fact]
    public void A_snapshot_taken_while_the_backup_runs_waits_for_a_new_trigger()
    {
      Assert.True(queue.Enqueue(1, new[] { Guid.NewGuid() }, new[] { "Docs" }));
      BackupQueue.Entry running = queue.Take(1)!;

      Assert.True(queue.Enqueue(1, new[] { Guid.NewGuid() }, new[] { "Docs" }));
      Assert.True(queue.Enqueue(2, new[] { Guid.NewGuid() }, new[] { "Docs" }));

      queue.Finish(running);
      queue.Cancel(1);
      queue.Cancel(2);
      Assert.All(pins.Values, count => Assert.Equal(0, count));
    }

    private void Change(IEnumerable<Guid> ids, int delta)
    {
      foreach (Guid id in ids)
        pins[id] = pins.GetValueOrDefault(id) + delta;
    }
  }
}
