using System.Runtime.Serialization;

namespace BitShelter.Models
{
  // Backups are encrypted as standard OpenPGP messages, so GnuPG or Kleopatra can decrypt them.
  [DataContract]
  public enum BackupEncryption
  {
    [EnumMember]
    None,
    [EnumMember]
    PgpPublicKey,
    [EnumMember]
    PgpPassphrase,
  }
}
