namespace YoutubeRag.Application.DTOs.Job;

/// <summary>
/// Simplified job data for list views
/// </summary>
public record JobListDto
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
    /// Gets the progress percentage (0-100)
    /// </summary>
    public int Progress { get; init; }

    /// <summary>
    /// Gets the video title if related to a video
    /// </summary>
    public string? VideoTitle { get; init; }

    /// <summary>
    /// Gets the video ID if related to a video
    /// </summary>
    public string? VideoId { get; init; }

    /// <summary>
    /// Gets when the job started
    /// </summary>
    public DateTime? StartedAt { get; init; }

    /// <summary>
    /// Gets when the job completed
    /// </summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>
    /// Gets when the job was created
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets the retry count
    /// </summary>
    public int RetryCount { get; init; }

    /// <summary>
    /// Gets whether the job has an error
    /// </summary>
    public bool HasError => Status == "Failed" && !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Gets a brief error message
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the duration of the job execution
    /// </summary>
    public TimeSpan? Duration => StartedAt.HasValue && CompletedAt.HasValue
        ? CompletedAt.Value - StartedAt.Value
        : null;
}
