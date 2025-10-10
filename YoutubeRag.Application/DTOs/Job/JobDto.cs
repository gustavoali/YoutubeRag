using YoutubeRag.Application.DTOs.User;
using YoutubeRag.Application.DTOs.Video;

namespace YoutubeRag.Application.DTOs.Job;

/// <summary>
/// Full job data transfer object
/// </summary>
public record JobDto
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
    /// Gets the job result data
    /// </summary>
    public string? Result { get; init; }

    /// <summary>
    /// Gets any error message
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the job parameters as JSON
    /// </summary>
    public string? Parameters { get; init; }

    /// <summary>
    /// Gets the job metadata as JSON
    /// </summary>
    public string? Metadata { get; init; }

    /// <summary>
    /// Gets when the job started
    /// </summary>
    public DateTime? StartedAt { get; init; }

    /// <summary>
    /// Gets when the job completed
    /// </summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>
    /// Gets the number of retry attempts
    /// </summary>
    public int RetryCount { get; init; }

    /// <summary>
    /// Gets the maximum number of retries allowed
    /// </summary>
    public int MaxRetries { get; init; }

    /// <summary>
    /// Gets the worker ID that processed this job
    /// </summary>
    public string? WorkerId { get; init; }

    /// <summary>
    /// Gets the user who created the job
    /// </summary>
    public UserProfileDto User { get; init; } = new();

    /// <summary>
    /// Gets the user ID
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the related video (if any)
    /// </summary>
    public VideoListDto? Video { get; init; }

    /// <summary>
    /// Gets the video ID (if any)
    /// </summary>
    public string? VideoId { get; init; }

    /// <summary>
    /// Gets when the job was created
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets when the job was last updated
    /// </summary>
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Gets the duration of the job execution
    /// </summary>
    public TimeSpan? Duration => StartedAt.HasValue && CompletedAt.HasValue
        ? CompletedAt.Value - StartedAt.Value
        : null;

    /// <summary>
    /// Gets whether the job can be retried
    /// </summary>
    public bool CanRetry => RetryCount < MaxRetries &&
        (Status == "Failed" || Status == "Cancelled");

    /// <summary>
    /// Gets whether the job can be cancelled
    /// </summary>
    public bool CanCancel => Status == "Pending" || Status == "Running" || Status == "Retrying";
}