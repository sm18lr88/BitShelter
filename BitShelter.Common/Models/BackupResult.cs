using System;

namespace BitShelter.Models
{
  // The outcome of one backup. The service keeps the most recent results and sends them to the Agent.
  public sealed class BackupResult
  {
    public long Id { get; set; }
    public long RuleId { get; set; }
    public string RuleName { get; set; }
    public string BackupName { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime FinishedAt { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; }
    public string OutputPath { get; set; }
    public long SizeBytes { get; set; }
    public int FileCount { get; set; }
    public int SkippedFileCount { get; set; }

    // True when the rule asks for a desktop notification for this outcome.
    public bool Notify { get; set; }

    // Only for logging inside the service. It is never saved or sent.
    [Newtonsoft.Json.JsonIgnore]
    public Exception Exception { get; set; }
  }
}
