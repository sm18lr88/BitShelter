using BitShelter.Models;
using BitShelter.Tests.Backup;
using BitShelter.Utils;
using BitShelter.VSS;
using BitShelter.VSS.Interop;
using Serilog.Events;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace BitShelter.Tests.Vss
{
  // Runs the VssClient call sequence against a fake IVssBackupComponents, so that error paths that a real
  // VSS service rarely produces are covered.
  [Collection(nameof(RealVssCollection))]
  public sealed partial class VssClientSequenceTests
  {
    private const int E_FAIL = unchecked((int)0x80004005);

    [Fact]
    public void Snapshot_ids_are_returned_when_a_writer_step_fails_after_the_snapshot_exists()
    {
      var components = FakeComponents.Create(out FakeComponents fake);
      fake.FailAfterSnapshot.Add("GatherWriterStatus");
      fake.FailAfterSnapshot.Add("BackupComplete");
      var host = new RecordingHost();
      using var vss = new VssClient(host, components, withWriters: true);

      List<Guid> ids = vss.CreateSnapshot(new[] { @"\\?\Volume{1}\", @"\\?\Volume{2}\" }).ToList();

      Assert.Equal(2, ids.Count);
      Assert.Contains("BackupComplete", fake.Calls);
      Assert.DoesNotContain("AbortBackup", fake.Calls);
      Assert.Equal(2, host.Warnings.Count);
    }

    // Privileged: creates a real shadow copy with VSS writers, makes the steps after DoSnapshotSet fail, and
    // deletes the shadow copy at the end. See VssBackupIntegrationTests.
    [Fact(Skip = "Set BITSHELTER_VSS_TESTS=1 in an elevated shell.", SkipUnless = nameof(VssBackupIntegrationTests.Enabled), SkipType = typeof(VssBackupIntegrationTests))]
    public void Real_snapshot_ids_are_returned_when_the_steps_after_the_snapshot_fail()
    {
      string volume = Volumes.GetUniqueVolumeNameForVolumeMountPoint(Volumes.GetVolumeRootPath(Path.GetTempPath()));
      IVssBackupComponents real = VssNative.CreateBackupComponents();
      VssException.ThrowIfFailed(real.InitializeForBackup(0), "InitializeForBackup");
      VssException.ThrowIfFailed(real.SetContext((int)VssSnapshotContextInternal.ClientAccessibleWriters), "SetContext");
      VssException.ThrowIfFailed(real.SetBackupState(false, false, VssNative.BackupTypeCopy, false), "SetBackupState");

      var host = new RecordingHost();
      List<Guid> ids;
      try
      {
        using var vss = new VssClient(host, FailingAfterSnapshot.Wrap(real), withWriters: true);
        ids = vss.CreateSnapshot(new[] { volume }).ToList();
      }
      finally
      {
        VssNative.Release(real);
      }

      using var check = new VssClient(new VssHost());
      check.Initialize(VssSnapshotContextInternal.All);
      try
      {
        Guid id = Assert.Single(ids);
        Assert.Equal(id, check.GetSnapshotProperties(id).SnapshotId);
        Assert.Equal(2, host.Warnings.Count);
      }
      finally
      {
        foreach (Guid id in ids)
          check.DeleteSnapshot(id);
      }
    }

    [Fact]
    public void Writer_metadata_is_freed_when_gathering_it_fails()
    {
      var components = FakeComponents.Create(out FakeComponents fake);
      fake.Results["GatherWriterMetadata"] = E_FAIL;
      using var vss = new VssClient(new RecordingHost(), components, withWriters: true);

      Assert.Throws<VssException>(() => vss.CreateSnapshot(new[] { @"\\?\Volume{1}\" }).ToList());
      Assert.Contains("FreeWriterMetadata", fake.Calls);
    }

    // Answers every call with S_OK (or a configured HRESULT) and a finished IVssAsync for async calls.
    // The calls in FailAfterSnapshot fail with E_FAIL once DoSnapshotSet has run.
    internal class FakeComponents : DispatchProxy
    {
      private static readonly StrategyBasedComWrappers Wrappers = new();

      public Dictionary<string, int> Results { get; } = new();
      public List<string> Calls { get; } = new();
      public HashSet<string> FailAfterSnapshot { get; } = new();

      public static IVssBackupComponents Create(out FakeComponents fake)
      {
        IVssBackupComponents proxy = Create<IVssBackupComponents, FakeComponents>();
        fake = (FakeComponents)(object)proxy;
        return proxy;
      }

      protected override object? Invoke(MethodInfo? method, object?[]? args)
      {
        bool snapshotExists = Calls.Contains("DoSnapshotSet");
        Calls.Add(method!.Name);
        int hr = snapshotExists && FailAfterSnapshot.Contains(method.Name) ? E_FAIL : Results.GetValueOrDefault(method.Name);
        ParameterInfo[] parameters = method.GetParameters();

        for (int i = 0; i < parameters.Length; i++)
        {
          if (!parameters[i].IsOut)
            continue;

          Type type = parameters[i].ParameterType.GetElementType()!;
          if (parameters[i].Name == "async" && hr >= 0)
            args![i] = Wrappers.GetOrCreateComInterfaceForObject(new FinishedAsync(), CreateComInterfaceFlags.None);
          else if (type == typeof(Guid))
            args![i] = Guid.NewGuid();
          else
            args![i] = Activator.CreateInstance(type);
        }

        return hr;
      }
    }

    // Forwards every call to the real VSS object, and fails GatherWriterStatus and BackupComplete once DoSnapshotSet has run.
    internal class FailingAfterSnapshot : DispatchProxy
    {
      private IVssBackupComponents real = null!;
      private bool snapshotDone;

      public static IVssBackupComponents Wrap(IVssBackupComponents real)
      {
        IVssBackupComponents proxy = Create<IVssBackupComponents, FailingAfterSnapshot>();
        ((FailingAfterSnapshot)(object)proxy).real = real;
        return proxy;
      }

      protected override object? Invoke(MethodInfo? method, object?[]? args)
      {
        if (snapshotDone && method!.Name is "GatherWriterStatus" or "BackupComplete")
        {
          args![0] = (nint)0;
          return E_FAIL;
        }

        object? result = method!.Invoke(real, args);
        snapshotDone |= method.Name == "DoSnapshotSet";
        return result;
      }
    }

    [GeneratedComClass]
    internal sealed partial class FinishedAsync : IVssAsync
    {
      public int Cancel() => 0;
      public int Wait(uint milliseconds) => 0;

      public int QueryStatus(out int result, nint reserved)
      {
        result = VssNative.AsyncFinished;
        return 0;
      }
    }

    private sealed class RecordingHost : IUIHost
    {
      public List<string> Warnings { get; } = new();

      public void WriteWarning(string message, params object[] args) => Warnings.Add(string.Format(message, args));
      public IDisposable GetIndent() => new MemoryStream();
      public void WriteLine() { }
      public void WriteTable(LogEventLevel level, StringTable table, int columnSpacing = 3, bool addRowSpace = false) { }
      public void PushIndent() { }
      public void PopIndent() { }
      public void WriteVerbose(string message, params object[] args) { }
      public void WriteDebugHeader(string message, params object[] args) { }
      public void WriteDebug(string message, params object[] args) { }
      public void WriteError(string message, params object[] args) { }
    }
  }
}
