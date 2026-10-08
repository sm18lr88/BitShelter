using BitShelter.Service.Config;
using System.Security.AccessControl;
using System.Security.Principal;

namespace BitShelter.Tests.Config
{
  public sealed class AppDataSecurityTests
  {
    private static readonly SecurityIdentifier LocalSystem = new(WellKnownSidType.LocalSystemSid, null);
    private static readonly SecurityIdentifier Administrators = new(WellKnownSidType.BuiltinAdministratorsSid, null);
    private static readonly SecurityIdentifier Users = new(WellKnownSidType.BuiltinUsersSid, null);

    [Fact]
    public void Only_system_and_administrators_have_access_and_inheritance_is_blocked()
    {
      DirectorySecurity security = AppDataSecurity.CreateDirectorySecurity();
      FileSystemAccessRule[] rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>().ToArray();

      Assert.True(security.AreAccessRulesProtected);
      Assert.Equal(new HashSet<IdentityReference> { LocalSystem, Administrators }, rules.Select(r => r.IdentityReference).ToHashSet());
      Assert.All(rules, r => Assert.Equal(FileSystemRights.FullControl, r.FileSystemRights));
      Assert.All(rules, r => Assert.Equal(InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, r.InheritanceFlags));
    }

    [Fact]
    public void Only_system_and_administrators_are_trusted_file_owners()
    {
      using WindowsIdentity current = WindowsIdentity.GetCurrent();

      Assert.True(AppDataSecurity.IsTrustedOwner(LocalSystem));
      Assert.True(AppDataSecurity.IsTrustedOwner(Administrators));
      Assert.False(AppDataSecurity.IsTrustedOwner(Users));
      Assert.False(AppDataSecurity.IsTrustedOwner(current.User));
      Assert.False(AppDataSecurity.IsTrustedOwner(null));
    }
  }
}
