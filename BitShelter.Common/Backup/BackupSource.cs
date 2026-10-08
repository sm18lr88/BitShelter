namespace BitShelter.Backup
{
  // One input folder of a backup rule. The service reads the files from the snapshot (ReadFolder)
  // but filters and names them by their normal path (InputFolder).
  public sealed class BackupSource
  {
    public string InputFolder { get; set; }
    public string ReadFolder { get; set; }
    public string Label { get; set; }

    public static BackupSource FromFolder(string inputFolder, string readFolder)
    {
      return new BackupSource { InputFolder = inputFolder, ReadFolder = readFolder, Label = BackupNaming.GetSourceLabel(inputFolder) };
    }
  }

  public sealed class BackupFile
  {
    public System.IO.FileInfo File { get; set; }
    public string LogicalPath { get; set; }
    public string EntryName { get; set; }
  }
}
