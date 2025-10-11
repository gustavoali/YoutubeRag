namespace YoutubeRag.Application.DTOs.Job;

/// <summary>
/// Simplified job status information
/// </summary>
public record JobStatusDto
{
    /// <summary>
    /// Gets the job's unique identifier
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets the job type
    /// </summary>
    public string JobType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the job status
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Gets the status message
    /// </summary>
    public string? StatusMessage { get; init; }

    /// <summary>
    /// Gets the progress percentage (0-100)
    /// </summary>
    public int Progress { get; init; }

    /// <summary>
    /// Gets when the job started
    /// </summary>
    public DateTime? StartedAt { get; init; }

    /// <summary>
    /// Gets when the job completed
    /// </summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>
    /// Gets the estimated time remaining
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; init; }

    /// <summary>
    /// Gets whether the job is complete
    /// </summary>
    public bool IsComplete => Status == "Completed" || Status == "Failed" || Status == "Cancelled";

    /// <summary>
    /// Gets whether the job is running
    /// </summary>
    public bool IsRunning => Status == "Running" || Status == "Retrying";
}
