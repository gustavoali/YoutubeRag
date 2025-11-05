namespace YoutubeRag.Application.DTOs.Video;

/// <summary>
/// Data transfer object for video submission result
/// </summary>
public record VideoSubmissionResultDto
{
    /// <summary>
    /// Gets the video ID
    /// </summary>
    public string VideoId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the job ID for background processing
    /// </summary>
    public string JobId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the video title
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Gets the video duration
    /// </summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>
    /// Gets the video author/channel name
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// Gets the thumbnail URL
    /// </summary>
    public string? ThumbnailUrl { get; init; }

    /// <summary>
    /// Gets the YouTube video ID
    /// </summary>
    public string YouTubeId { get; init; } = string.Empty;

    /// <summary>
    /// Gets whether this is an existing video (duplicate)
    /// </summary>
    public bool IsExisting { get; init; }

    /// <summary>
    /// Gets a message about the submission
    /// </summary>
    public string Message { get; init; } = string.Empty;
}
