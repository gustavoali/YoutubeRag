namespace YoutubeRag.Domain.Entities;

/// <summary>
/// Represents a job that has failed after all retry attempts and requires manual intervention
/// </summary>
public class DeadLetterJob : BaseEntity
{
    /// <summary>
    /// Foreign key to the original failed job
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// High-level failure reason (e.g., "MaxRetriesExceeded", "PermanentError")
    /// </summary>
    public string FailureReason { get; set; } = string.Empty;

    /// <summary>
    /// Detailed failure information including exception details, stack trace, etc. (JSON serialized)
    /// </summary>
    public string? FailureDetails { get; set; }

    /// <summary>
    /// Original job payload/parameters for potential reprocessing (JSON serialized)
    /// </summary>
    public string? OriginalPayload { get; set; }

    /// <summary>
    /// Timestamp when the job was moved to the dead letter queue
    /// </summary>
    public DateTime FailedAt { get; set; }

    /// <summary>
    /// Number of retry attempts made before moving to DLQ
    /// </summary>
    public int AttemptedRetries { get; set; }

    /// <summary>
    /// Indicates if this dead letter job has been requeued for retry
    /// </summary>
    public bool IsRequeued { get; set; } = false;

    /// <summary>
    /// Timestamp when the job was requeued (if applicable)
    /// </summary>
    public DateTime? RequeuedAt { get; set; }

    /// <summary>
    /// User or system that triggered the requeue operation
    /// </summary>
    public string? RequeuedBy { get; set; }

    /// <summary>
    /// Notes or comments about the failure or resolution
    /// </summary>
    public string? Notes { get; set; }

    // Navigation Properties
    /// <summary>
    /// Navigation property to the original failed job
    /// </summary>
    public virtual Job Job { get; set; } = null!;
}
