using YoutubeRag.Application.DTOs.User;

namespace YoutubeRag.Application.DTOs.Video;

/// <summary>
/// Full video data transfer object with navigation properties
/// </summary>
public record VideoDto
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
    /// Gets the video description
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the YouTube video ID
    /// </summary>
    public string? YoutubeId { get; init; }

    /// <summary>
    /// Gets the YouTube URL
    /// </summary>
    public string? YoutubeUrl { get; init; }

    /// <summary>
    /// Gets the original URL
    /// </summary>
    public string? OriginalUrl { get; init; }

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
    /// Gets the like count
    /// </summary>
    public int? LikeCount { get; init; }

    /// <summary>
    /// Gets the video processing status
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Gets the file path for uploaded video
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Gets the audio file path
    /// </summary>
    public string? AudioPath { get; init; }

    /// <summary>
    /// Gets the processing progress percentage
    /// </summary>
    public int ProcessingProgress { get; init; }

    /// <summary>
    /// Gets any error message from processing
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the video metadata as JSON
    /// </summary>
    public string? Metadata { get; init; }

    /// <summary>
    /// Gets the user who owns the video
    /// </summary>
    public UserProfileDto User { get; init; } = new();

    /// <summary>
    /// Gets the user ID
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Gets when the video was created
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets when the video was last updated
    /// </summary>
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Gets the count of transcript segments
    /// </summary>
    public int TranscriptSegmentCount { get; init; }

    /// <summary>
    /// Gets the count of related jobs
    /// </summary>
    public int JobCount { get; init; }
}