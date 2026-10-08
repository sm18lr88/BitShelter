using BitShelter.Models;
using SharpCompress.Common;

namespace BitShelter.Benchmark.Models
{
  public class CEBenchResult
  {
    public ArchiveType ArchiveType { get; set; }
    public CompressionType CompressionType { get; set; }
    public BackupEncryption Encryption { get; set; }
    public EncryptionAlgorithm EncryptionAlgorithm { get; set; }

    public long CompressedSize { get; set; }
    public long TotalRuntime { get; set; }
  }
}
