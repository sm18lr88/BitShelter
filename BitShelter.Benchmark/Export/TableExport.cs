using BitShelter.Benchmark.Models;
using BitShelter.Encryption;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace BitShelter.Benchmark.Export
{
  public class TableExport
  {
    private static readonly string[] Header =
    {
      "Archive", "Compression", "Encryption", "Cipher", "Compressed Size",
      "Compression Ratio (%)", "Total Runtime", "Mean Runtime"
    };

    protected IEnumerable<CEBenchResult> Results { get; }
    public int OriginalSize { get; }
    public int Iterations { get; }

    public TableExport(IEnumerable<CEBenchResult> results, int originalSize, int iterations)
    {
      Results = results;
      OriginalSize = originalSize;
      Iterations = iterations;
    }

    public void ExportCsv(string filepath)
    {
      using (var writer = new StreamWriter(filepath))
      {
        writer.WriteLine(string.Join(",", Header));

        foreach (CEBenchResult res in Results)
          writer.WriteLine(string.Join(",", ToRow(res).Select(Quote)));
      }
    }

    private IEnumerable<string> ToRow(CEBenchResult res)
    {
      double ratio = res.CompressedSize == 0 ? 100d : (double)res.CompressedSize / OriginalSize * 100d;

      yield return res.ArchiveType.ToString();
      yield return res.CompressionType.ToString();
      yield return res.Encryption.ToString();
      yield return res.EncryptionAlgorithm.GetDisplayName();
      yield return res.CompressedSize.ToString(CultureInfo.InvariantCulture);
      yield return ratio.ToString("F2", CultureInfo.InvariantCulture);
      yield return res.TotalRuntime.ToString(CultureInfo.InvariantCulture);
      yield return (res.TotalRuntime / Iterations).ToString(CultureInfo.InvariantCulture);
    }

    private static string Quote(string value)
    {
      return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
  }
}
