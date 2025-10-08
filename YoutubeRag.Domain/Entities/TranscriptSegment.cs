namespace YoutubeRag.Domain.Entities;

public class TranscriptSegment : BaseEntity
{
    public string VideoId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public int SegmentIndex { get; set; }
    public string? EmbeddingVector { get; set; }
    public double? Confidence { get; set; }
    public string? Language { get; set; }
    public string? Speaker { get; set; } // Optional speaker identification

    // Computed Properties
    public bool HasEmbedding => !string.IsNullOrWhiteSpace(EmbeddingVector);

    // Navigation Properties
    public virtual Video Video { get; set; } = null!;
}