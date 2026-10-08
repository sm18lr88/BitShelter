using BitShelter.Models;
using BitShelter.Service.Backup;
using System.Security.AccessControl;
using System.Security.Principal;

namespace BitShelter.Tests.Backup
{
  // Changing a file owner to Administrators needs an elevated shell (see TESTING.md).
  public sealed class BackupStateMgrTests : IDisposable
  {
    private readonly string folder = Directory.CreateTempSubdirectory("bitshelter-state-tests-").FullName;

    public void Dispose() => Directory.Delete(folder, recursive: true);

    [Fact]
    public void Snapshot_numbers_continue_after_a_reload_of_a_trusted_file()
    {
      var state = new BackupStateMgr();
      state.Load(folder);

      Assert.Equal(0, state.NextSnapshotNumber(7));
      Assert.Equal(1, state.NextSnapshotNumber(7));
      Assert.Equal(0, state.NextSnapshotNumber(8));

      SetOwner(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null));
      var reloaded = new BackupStateMgr();
      reloaded.Load(folder);

      Assert.Equal(2, reloaded.NextSnapshotNumber(7));
    }

    [Fact]
    public void A_state_file_owned_by_a_user_account_is_ignored()
    {
      var state = new BackupStateMgr();
      state.Load(folder);
      state.NextSnapshotNumber(7);

      using (WindowsIdentity current = WindowsIdentity.GetCurrent())
        SetOwner(current.User!);
      var reloaded = new BackupStateMgr();
      reloaded.Load(folder);

      Assert.Equal(0, reloaded.NextSnapshotNumber(7));
    }

    [Fact]
    public void Results_get_increasing_ids_and_only_the_newest_are_kept()
    {
      var state = new BackupStateMgr();
      state.Load(folder);

      for (int i = 0; i < BackupStateMgr.MaxResults + 5; i++)
        state.AddResult(new BackupResult { BackupName = "b" + i });

      IReadOnlyList<BackupResult> all = state.GetResultsSince(0);
      Assert.Equal(BackupStateMgr.MaxResults, all.Count);
      Assert.Equal(6, all[0].Id);
      Assert.Equal(new long[] { BackupStateMgr.MaxResults + 5 }, state.GetResultsSince(BackupStateMgr.MaxResults + 4).Select(r => r.Id));
    }

    private void SetOwner(SecurityIdentifier owner)
    {
      var file = new FileInfo(Path.Combine(folder, BackupStateMgr.FileName));
      FileSecurity security = file.GetAccessControl();
      security.SetOwner(owner);
      file.SetAccessControl(security);
    }
  }
}
