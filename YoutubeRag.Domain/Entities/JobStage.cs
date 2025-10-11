using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Domain.Entities;

/// <summary>
/// Represents a single stage within a job pipeline
/// </summary>
public class JobStage : BaseEntity
{
    public string JobId { get; set; } = string.Empty;
    public string StageName { get; set; } = string.Empty;
    public StageStatus Status { get; set; } = StageStatus.Pending;
    public int Progress { get; set; } = 0;
    public int Order { get; set; }
    public int Weight { get; set; } = 1; // Weight for overall progress calculation
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorDetails { get; set; } // JSON serialized error details
    public string? InputData { get; set; } // JSON serialized input
    public string? OutputData { get; set; } // JSON serialized output
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
    public DateTime? NextRetryAt { get; set; }
    public TimeSpan? EstimatedDuration { get; set; }
    public string? Metadata { get; set; } // JSON for additional stage-specific data

    // Navigation Properties
    public virtual Job Job { get; set; } = null!;

    // Computed Properties
    public TimeSpan? ActualDuration =>
        StartedAt.HasValue && CompletedAt.HasValue
            ? CompletedAt.Value - StartedAt.Value
            : null;

    public bool CanRetry =>
        Status == StageStatus.Failed &&
        RetryCount < MaxRetries;

    public bool IsTerminal =>
        Status == StageStatus.Completed ||
        Status == StageStatus.Skipped ||
        (Status == StageStatus.Failed && !CanRetry);
}
