using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace BitShelter.Service.Config
{
  // The service runs as LocalSystem and acts on the rule and state files in %ProgramData%\BitShelter.
  // If a standard user could write there, a planted rule file could make the service delete snapshots
  // or copy protected files. Only SYSTEM and Administrators may access the folder.
  internal static class AppDataSecurity
  {
    private static readonly SecurityIdentifier LocalSystem = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
    private static readonly SecurityIdentifier Administrators = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);

    internal static DirectorySecurity CreateDirectorySecurity()
    {
      var security = new DirectorySecurity();
      const InheritanceFlags inherit = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;

      security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
      security.AddAccessRule(new FileSystemAccessRule(LocalSystem, FileSystemRights.FullControl, inherit, PropagationFlags.None, AccessControlType.Allow));
      security.AddAccessRule(new FileSystemAccessRule(Administrators, FileSystemRights.FullControl, inherit, PropagationFlags.None, AccessControlType.Allow));

      return security;
    }

    // Windows propagates the new inheritable entries to the existing files and subfolders.
    public static void Apply(string folderPath)
    {
      new DirectoryInfo(folderPath).SetAccessControl(CreateDirectorySecurity());
    }

    // A file that a standard user created before the folder was locked must not be loaded.
    public static bool HasTrustedOwner(string filePath)
    {
      var owner = new FileInfo(filePath).GetAccessControl().GetOwner(typeof(SecurityIdentifier)) as SecurityIdentifier;

      return IsTrustedOwner(owner);
    }

    internal static bool IsTrustedOwner(SecurityIdentifier owner)
    {
      return owner != null && (owner.Equals(LocalSystem) || owner.Equals(Administrators));
    }
  }
}
