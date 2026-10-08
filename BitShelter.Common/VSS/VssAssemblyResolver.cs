using Alphaleonis.Win32.Vss;
using System;
using System.IO;
using System.Reflection;

namespace BitShelter.VSS
{
  internal sealed class VssAssemblyResolver : IVssAssemblyResolver
  {
    public Assembly LoadAssembly(AssemblyName assemblyName)
    {
      if (assemblyName == null)
        throw new ArgumentNullException(nameof(assemblyName));

      try
      {
        return Assembly.Load(assemblyName);
      }
      catch (Exception ex) when (ex is FileNotFoundException or FileLoadException)
      {
        // Not on the default probing path; fall through to the runtime-specific locations below.
      }

      string fileName = assemblyName.Name + ".dll";
      string baseDir = AppContext.BaseDirectory;

      string[] candidates =
      {
        Path.Combine(baseDir, fileName),
        Path.Combine(baseDir, "runtimes", "win-x64", "native", fileName),
        Path.Combine(baseDir, "runtimes", "win-x86", "native", fileName)
      };

      foreach (string candidate in candidates)
      {
        if (File.Exists(candidate))
          return Assembly.LoadFrom(candidate);
      }

      throw new FileNotFoundException($"Could not resolve VSS assembly '{assemblyName.FullName}'.", fileName);
    }
  }
}

