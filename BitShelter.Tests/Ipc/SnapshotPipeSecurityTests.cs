using BitShelter.Service.Ipc;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;

namespace BitShelter.Tests.Ipc
{
  public sealed class SnapshotPipeSecurityTests
  {
    private static readonly SecurityIdentifier Administrators = new(WellKnownSidType.BuiltinAdministratorsSid, null);

    private static PipeAccessRule[] GetRules()
    {
      return SnapshotPipeServer.CreatePipeSecurity()
        .GetAccessRules(includeExplicit: true, includeInherited: false, typeof(SecurityIdentifier))
        .Cast<PipeAccessRule>()
        .ToArray();
    }

    [Fact]
    public void Only_service_account_and_administrators_are_granted_access()
    {
      using WindowsIdentity current = WindowsIdentity.GetCurrent();

      var grantees = GetRules().Select(r => r.IdentityReference).ToHashSet();

      Assert.Equal(new HashSet<IdentityReference> { current.User!, Administrators }, grantees);
    }

    [Fact]
    public void Administrators_can_read_and_write_but_not_create_pipe_instances()
    {
      PipeAccessRule admins = GetRules().Single(r => r.IdentityReference.Equals(Administrators));

      Assert.Equal(AccessControlType.Allow, admins.AccessControlType);
      Assert.Equal(PipeAccessRights.ReadWrite, admins.PipeAccessRights & PipeAccessRights.ReadWrite);
      Assert.Equal((PipeAccessRights)0, admins.PipeAccessRights & PipeAccessRights.CreateNewInstance);
    }
  }
}
