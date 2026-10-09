using BitShelter.Backup;
using BitShelter.Models;
using BitShelter.Service.Backup;
using BitShelter.Utils;
using BitShelter.VSS;

namespace BitShelter.Tests.Backup
{
  // Privileged test: it creates a real shadow copy and deletes it at the end.
  // Run it from an elevated shell with BITSHELTER_VSS_TESTS=1 (see TESTING.md).
  [Collection(nameof(Vss.RealVssCollection))]
  public sealed class VssBackupIntegrationTests : IDisposable
  {
    private readonly string root = Directory.CreateTempSubdirectory("bitshelter-vss-tests-").FullName;

    public static bool Enabled => Environment.GetEnvironmentVariable("BITSHELTER_VSS_TESTS") == "1";

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Theory(Skip = "Set BITSHELTER_VSS_TESTS=1 in an elevated shell. The test creates and deletes a shadow copy.", SkipUnless = nameof(Enabled))]
    [InlineData(VssSnapshotContextInternal.ClientAccessible)]
    [InlineData(VssSnapshotContextInternal.ClientAccessibleWriters)]
    public void Backup_copies_the_files_as_they_were_when_the_snapshot_was_created(VssSnapshotContextInternal context)
    {
      string input = Directory.CreateDirectory(Path.Combine(root, "input")).FullName;
      string output = Path.Combine(root, "output");
      string file = Path.Combine(input, "doc.txt");
      File.WriteAllText(file, "in the snapshot");

      string volume = Volumes.GetUniqueVolumeNameForVolumeMountPoint(Volumes.GetVolumeRootPath(input));
      using var vss = new VssClient(new VssHost());
      vss.Initialize(context);
      List<Guid> ids = vss.CreateSnapshot(new[] { volume }).ToList();

      try
      {
        File.WriteAllText(file, "changed after the snapshot");

        List<VssSnapshotProperties> snapshots = ids.Select(vss.GetSnapshotProperties).ToList();
        Assert.All(snapshots, s => Assert.Equal(context == VssSnapshotContextInternal.ClientAccessibleWriters, s.CreatedWithWriters));
        var backup = new BackupRule { Name = "Docs", InputFolders = new HashSet<string> { input }, OutputFolder = output, CompressionEnabled = false };
        var rule = new SnapshotRule { Id = 1, Name = "VSS test", BackupRules = new List<BackupRule> { backup } };

        BackupResult result = BackupEngine.Run(rule, backup, SnapshotBackupSources.Create(backup.InputFolders, snapshots), DateTime.Now, CancellationToken.None);

        string copied = Path.Combine(result.OutputPath, BackupNaming.GetSourceLabel(input).Replace('/', '\\'), "doc.txt");
        Assert.Equal("in the snapshot", File.ReadAllText(copied));
      }
      finally
      {
        foreach (Guid id in ids)
          vss.DeleteSnapshot(id);
      }
    }
  }
}
