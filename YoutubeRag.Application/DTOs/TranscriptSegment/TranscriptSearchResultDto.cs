namespace YoutubeRag.Application.DTOs.TranscriptSegment;

/// <summary>
/// Search result for transcript segments
/// </summary>
public record TranscriptSearchResultDto
{
    /// <summary>
    /// Gets the segment's unique identifier
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets the video ID
    /// </summary>
    public string VideoId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the video title
    /// </summary>
    public string VideoTitle { get; init; } = string.Empty;

    /// <summary>
    /// Gets the video thumbnail URL
    /// </summary>
    public string? VideoThumbnailUrl { get; init; }

    /// <summary>
    /// Gets the matched transcript text with highlights
    /// </summary>
    public string HighlightedText { get; init; } = string.Empty;

    /// <summary>
    /// Gets the original transcript text
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// Gets the start time in seconds
    /// </summary>
    public double StartTime { get; init; }

    /// <summary>
    /// Gets the end time in seconds
    /// </summary>
    public double EndTime { get; init; }

    /// <summary>
    /// Gets the relevance score (for semantic search)
    /// </summary>
    public double? RelevanceScore { get; init; }

    /// <summary>
    /// Gets the formatted timestamp
    /// </summary>
    public string Timestamp => TimeSpan.FromSeconds(StartTime).ToString(@"hh\:mm\:ss");

    /// <summary>
    /// Gets the YouTube URL with timestamp
    /// </summary>
    public string? YouTubeTimestampUrl { get; init; }

    /// <summary>
    /// Gets the context segments (before and after)
    /// </summary>
    public IReadOnlyList<TranscriptSegmentListDto> ContextSegments { get; init; } = new List<TranscriptSegmentListDto>();
}