using System;
using System.Security.Cryptography;
using System.Text;

namespace BitShelter.Encryption
{
  // Protects backup passphrases with DPAPI (local machine scope). The Agent protects the passphrase
  // before it sends the rule, so the plain passphrase never goes over the pipe or to disk.
  // Any process on this computer that can read the rule file can unprotect it, so the service
  // restricts %ProgramData%\BitShelter to SYSTEM and Administrators.
  public static class PassphraseProtector
  {
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("BitShelter.BackupPassphrase.v1");

    public static string Protect(string passphrase)
    {
      byte[] data = Encoding.UTF8.GetBytes(passphrase ?? throw new ArgumentNullException(nameof(passphrase)));

      return Convert.ToBase64String(ProtectedData.Protect(data, Entropy, DataProtectionScope.LocalMachine));
    }

    public static string Unprotect(string protectedPassphrase)
    {
      byte[] data = Convert.FromBase64String(protectedPassphrase ?? throw new ArgumentNullException(nameof(protectedPassphrase)));

      return Encoding.UTF8.GetString(ProtectedData.Unprotect(data, Entropy, DataProtectionScope.LocalMachine));
    }
  }
}
