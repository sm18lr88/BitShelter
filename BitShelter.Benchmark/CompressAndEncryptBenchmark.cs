using BitShelter.Benchmark.Models;
using BitShelter.Encryption;
using BitShelter.IO;
using BitShelter.Models;
using SharpCompress.Common;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace BitShelter.Benchmark
{
  // Measures each archive, compression, and OpenPGP encryption combination that backups can use.
  public static class CompressAndEncryptBenchmark
  {
    public static IEnumerable<CEBenchResult> Run(Stream inStream, int iterationNb)
    {
      var results = new List<CEBenchResult>();
      var stopwatch = new Stopwatch();

      foreach (ArchiveType archiveType in CompressionHelper.AvailableArchives)
        foreach (CompressionType compressionType in archiveType.GetAvailableCompressions())
          foreach ((BackupEncryption encryption, EncryptionAlgorithm algorithm) in GetEncryptions())
          {
            long compressedSize = 0;

            stopwatch.Restart();

            for (int i = 0; i < iterationNb; i++)
            {
              inStream.Seek(0, SeekOrigin.Begin);
              compressedSize = Process(inStream, archiveType, compressionType, encryption, algorithm);
            }

            stopwatch.Stop();

            results.Add(new CEBenchResult
            {
              ArchiveType = archiveType,
              CompressionType = compressionType,
              Encryption = encryption,
              EncryptionAlgorithm = algorithm,
              TotalRuntime = stopwatch.ElapsedMilliseconds,
              CompressedSize = compressedSize,
            });
          }

      return results;
    }

    private static IEnumerable<(BackupEncryption, EncryptionAlgorithm)> GetEncryptions()
    {
      yield return (BackupEncryption.None, EncryptionAlgorithm.None);

      foreach (EncryptionAlgorithm cipher in OpenPgpEncryption.Ciphers)
      {
        yield return (BackupEncryption.PgpPassphrase, cipher);
        yield return (BackupEncryption.PgpPublicKey, cipher);
      }
    }

    // Returns the size of the archive file, after encryption.
    public static long Process(Stream data, ArchiveType archiveType, CompressionType compressionType, BackupEncryption encryption, EncryptionAlgorithm algorithm)
    {
      string fileName = Path.GetTempFileName();

      try
      {
        using (Stream file = File.Create(fileName))
        using (Stream encrypted = OpenEncryption(file, encryption, algorithm))
          CompressionHelper.Compress("data.bin", data, encrypted ?? file, archiveType, compressionType, null);

        return new FileInfo(fileName).Length;
      }
      finally
      {
        File.Delete(fileName);
      }
    }

    private static Stream OpenEncryption(Stream output, BackupEncryption encryption, EncryptionAlgorithm algorithm)
    {
      return encryption switch
      {
        BackupEncryption.PgpPassphrase => OpenPgpEncryption.OpenForPassphrase(output, Const.Passphrase, algorithm, "data.bin"),
        BackupEncryption.PgpPublicKey => OpenPgpEncryption.OpenForPublicKey(output, Const.PGPPubKey, algorithm, "data.bin"),
        _ => null,
      };
    }
  }
}
