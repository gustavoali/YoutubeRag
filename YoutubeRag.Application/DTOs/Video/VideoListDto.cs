namespace YoutubeRag.Application.DTOs.Video;

/// <summary>
/// Simplified video data for list views
/// </summary>
public record VideoListDto
{
    /// <summary>
    /// Gets the video's unique identifier
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets the video title
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Gets the video description (truncated)
    /// </summary>
    public string? DescriptionSnippet { get; init; }

    /// <summary>
    /// Gets the thumbnail URL
    /// </summary>
    public string? ThumbnailUrl { get; init; }

    /// <summary>
    /// Gets the video duration
    /// </summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>
    /// Gets the view count
    /// </summary>
    public int? ViewCount { get; init; }

    /// <summary>
    /// Gets the processing status
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Gets the processing progress percentage
    /// </summary>
    public int ProcessingProgress { get; init; }

    /// <summary>
    /// Gets the owner's name
    /// </summary>
    public string OwnerName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the owner's ID
    /// </summary>
    public string OwnerId { get; init; } = string.Empty;

    /// <summary>
    /// Gets when the video was created
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets whether the video has transcripts
    /// </summary>
    public bool HasTranscripts { get; init; }

    /// <summary>
    /// Gets whether the video is from YouTube
    /// </summary>
    public bool IsYouTubeVideo { get; init; }
}