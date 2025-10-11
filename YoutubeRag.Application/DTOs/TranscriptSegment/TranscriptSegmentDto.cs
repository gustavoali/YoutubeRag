namespace YoutubeRag.Application.DTOs.TranscriptSegment;

/// <summary>
/// Full transcript segment data transfer object
/// </summary>
public record TranscriptSegmentDto
{
    /// <summary>
    /// Gets the segment's unique identifier
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets the video ID this segment belongs to
    /// </summary>
    public string VideoId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the transcript text
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
    /// Gets the segment index (order in the video)
    /// </summary>
    public int SegmentIndex { get; init; }

    /// <summary>
    /// Gets the confidence score of the transcription
    /// </summary>
    public double? Confidence { get; init; }

    /// <summary>
    /// Gets the language code
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// Gets whether this segment has an embedding vector
    /// </summary>
    public bool HasEmbedding { get; init; }

    /// <summary>
    /// Gets the embedding vector (serialized)
    /// </summary>
    public string? EmbeddingVector { get; init; }

    /// <summary>
    /// Gets when the segment was created
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets when the segment was last updated
    /// </summary>
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Gets the duration of this segment in seconds
    /// </summary>
    public double Duration => EndTime - StartTime;

    /// <summary>
    /// Gets the formatted start time (HH:mm:ss)
    /// </summary>
    public string FormattedStartTime => TimeSpan.FromSeconds(StartTime).ToString(@"hh\:mm\:ss");

    /// <summary>
    /// Gets the formatted end time (HH:mm:ss)
    /// </summary>
    public string FormattedEndTime => TimeSpan.FromSeconds(EndTime).ToString(@"hh\:mm\:ss");
}
