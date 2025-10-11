namespace YoutubeRag.Application.DTOs.TranscriptSegment;

/// <summary>
/// Simplified transcript segment for list views
/// </summary>
public record TranscriptSegmentListDto
{
    /// <summary>
    /// Gets the segment's unique identifier
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets the transcript text (truncated for list view)
    /// </summary>
    public string TextSnippet { get; init; } = string.Empty;

    /// <summary>
    /// Gets the start time in seconds
    /// </summary>
    public double StartTime { get; init; }

    /// <summary>
    /// Gets the end time in seconds
    /// </summary>
    public double EndTime { get; init; }

    /// <summary>
    /// Gets the segment index
    /// </summary>
    public int SegmentIndex { get; init; }

    /// <summary>
    /// Gets the confidence score
    /// </summary>
    public double? Confidence { get; init; }

    /// <summary>
    /// Gets whether this segment has an embedding
    /// </summary>
    public bool HasEmbedding { get; init; }

    /// <summary>
    /// Gets the formatted time range (HH:mm:ss - HH:mm:ss)
    /// </summary>
    public string TimeRange => $"{TimeSpan.FromSeconds(StartTime):hh\\:mm\\:ss} - {TimeSpan.FromSeconds(EndTime):hh\\:mm\\:ss}";

    /// <summary>
    /// Gets the duration in seconds
    /// </summary>
    public double Duration => EndTime - StartTime;
}
