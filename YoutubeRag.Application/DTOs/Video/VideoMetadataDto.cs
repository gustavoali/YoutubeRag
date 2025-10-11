namespace YoutubeRag.Application.DTOs.Video;

/// <summary>
/// Data transfer object for video metadata extracted from YouTube
/// </summary>
public class VideoMetadataDto
{
    /// <summary>
    /// Video title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Video description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Video duration
    /// </summary>
    public TimeSpan? Duration { get; set; }

    /// <summary>
    /// Number of views
    /// </summary>
    public int? ViewCount { get; set; }

    /// <summary>
    /// Number of likes
    /// </summary>
    public int? LikeCount { get; set; }

    /// <summary>
    /// Date and time when the video was published
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// YouTube channel ID
    /// </summary>
    public string? ChannelId { get; set; }

    /// <summary>
    /// YouTube channel title
    /// </summary>
    public string? ChannelTitle { get; set; }

    /// <summary>
    /// List of thumbnail URLs in different resolutions
    /// </summary>
    public List<string> ThumbnailUrls { get; set; } = new();

    /// <summary>
    /// Video tags
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// YouTube category ID
    /// </summary>
    public string? CategoryId { get; set; }

    /// <summary>
    /// Gets the highest resolution thumbnail URL (maxresdefault)
    /// </summary>
    public string? ThumbnailUrl => ThumbnailUrls.FirstOrDefault();

    /// <summary>
    /// Duration in seconds for validation purposes
    /// </summary>
    public int DurationSeconds => (int)(Duration?.TotalSeconds ?? 0);
}
