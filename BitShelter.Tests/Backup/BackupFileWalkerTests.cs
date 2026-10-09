using BitShelter.Backup;

namespace BitShelter.Tests.Backup
{
  public sealed class BackupFileWalkerTests : IDisposable
  {
    private readonly string root = Directory.CreateTempSubdirectory("bitshelter-walker-tests-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void File_symbolic_links_are_not_followed()
    {
      string input = Directory.CreateDirectory(Path.Combine(root, "input")).FullName;
      File.WriteAllText(Path.Combine(input, "real.txt"), "real");
      string outside = Path.Combine(root, "outside.txt");
      File.WriteAllText(outside, "not in the input folder");

      try
      {
        File.CreateSymbolicLink(Path.Combine(input, "link.txt"), outside);
      }
      catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
      {
        Assert.Skip("Creating a symbolic link needs Developer Mode or an elevated shell: " + ex.Message);
      }

      var source = new BackupSource { InputFolder = input, ReadFolder = input, Label = "input" };

      List<string> entries = BackupFileWalker.Enumerate(source, null, null, null, CancellationToken.None).Select(f => f.EntryName).ToList();

      Assert.Equal(new[] { "input/real.txt" }, entries);
    }
  }
}
