using System.Collections.Generic;

namespace HorizonXIBackupApp.Models;

public enum UpdateStatus
{
    Unknown,
    Skipped,
    Error,
    FirstSeen,
    Unchanged,
    New
}

public sealed class AddonCheckResult
{
    public string Name { get; set; } = "";
    public UpdateStatus Status { get; set; } = UpdateStatus.Unknown;
    public string? PrevSha { get; set; }
    public string? Sha { get; set; }
    public string? Date { get; set; }
    public string? Message { get; set; }
    public string? CommitUrl { get; set; }
    public string? Url { get; set; }
    public string? Detail { get; set; }
}

public sealed class UpdateCheckSummary
{
    public List<AddonCheckResult> Results { get; set; } = new();
    public int NewCount { get; set; }
    public int UnchangedCount { get; set; }
    public int FirstSeenCount { get; set; }
    public int UnknownCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public int IgnoredCount { get; set; }
}
