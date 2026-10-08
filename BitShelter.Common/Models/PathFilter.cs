using DotNet.Globbing;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace BitShelter.Models
{
  [DataContract]
  public class PathFilter
  {
    [DataMember]
    public FilterPatternType FilterPatternType { get; set; }
    [DataMember]
    public string Pattern { get; set; }

    [JsonIgnore]
    public Glob Glob { get; set; }
    [JsonIgnore]
    public Regex Regex { get; set; }

    public override bool Equals(object obj)
    {
      if (obj == null || !(obj is PathFilter))
        return false;

      var other = (PathFilter)obj;

      if (Pattern == null)
        return other.Pattern == null;

      return Pattern.Equals(other.Pattern);
    }

    public override int GetHashCode()
    {
      return Pattern?.GetHashCode() ?? 0;
    }
  }
}
