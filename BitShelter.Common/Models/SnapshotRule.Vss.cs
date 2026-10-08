using BitShelter.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BitShelter.Models
{
  partial class SnapshotRule
  {
    [DataMember]
    public int LifeTimeValue { get; set; }
    [DataMember]
    public Timespan LifeTimeUnit { get; set; }
    [DataMember]
    public List<string> Volumes { get; set; } = new List<string>();
    
    public string VolumesAsString => String.Join(",",
        BitShelter.Utils.Volumes.GetVolumesForDevicesID(Volumes)
        .Select(v => v.ToShortString())
      );

    [DataMember]
    public VssSnapshotContextInternal VssContext { get; set; } = VssSnapshotContextInternal.ClientAccessible;

    // Rules from earlier versions can hold other contexts. A context with VSS writers maps to ClientAccessibleWriters,
    // any other to ClientAccessible: these are the two persistent contexts that File Explorer shows as Previous Versions.
    public bool UsesVssWriters => VssContext != VssSnapshotContextInternal.All && ((uint)VssContext & 0x10) == 0;

    public VssSnapshotContextInternal SnapshotContext => UsesVssWriters
      ? VssSnapshotContextInternal.ClientAccessibleWriters
      : VssSnapshotContextInternal.ClientAccessible;

    // Rules saved before this setting existed load as Local, which keeps their earlier behavior.
    [DataMember]
    public PruningStrategy PruningStrategy { get; set; } = PruningStrategy.Local;

    [DataMember]
    public int MaxRetryCount { get; set; }
    [DataMember]
    public bool RetryRestartVSSService { get; set; }

    [DataMember]
    public int ChecksumCanary { get; set; }
    [DataMember]
    public bool ChecksumFiles { get; set; }
    [DataMember]
    public HashSet<PathFilter> ChecksumFileIncludes { get; set; } = new HashSet<PathFilter>();
    [DataMember]
    public HashSet<PathFilter> ChecksumFileExcludes { get; set; } = new HashSet<PathFilter>();
  }
}
