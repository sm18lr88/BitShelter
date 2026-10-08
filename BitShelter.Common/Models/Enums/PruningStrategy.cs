using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BitShelter.Models.Enums
{
  // Local: delete the snapshots of a rule only when they are older than its lifetime.
  // Global: also, before each snapshot, when a volume is at its snapshot limit, delete the oldest snapshot
  // of this rule on that volume. Otherwise VSS deletes the oldest snapshot of the volume, which can be
  // a System Restore point or a snapshot of another rule.
  [DataContract]
  public enum PruningStrategy
  {
    [EnumMember]
    Local,
    [EnumMember]
    Global
  }
}
