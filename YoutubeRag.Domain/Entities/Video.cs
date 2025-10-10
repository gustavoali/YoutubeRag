using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Domain.Entities;

public class Video : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? YouTubeId { get; set; }
    public string? Url { get; set; }
    public string? OriginalUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public TimeSpan? Duration { get; set; }
    public int? ViewCount { get; set; }
    public int? LikeCount { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? ChannelId { get; set; }
    public string? ChannelTitle { get; set; }
    public string? CategoryId { get; set; }
    public List<string> Tags { get; set; } = new();
    public VideoStatus Status { get; set; } = VideoStatus.Pending;
    public VideoStatus ProcessingStatus { get; set; } = VideoStatus.Pending;
    public TranscriptionStatus TranscriptionStatus { get; set; } = TranscriptionStatus.NotStarted;
    public string? FilePath { get; set; }
    public string? AudioPath { get; set; }
    public string? ProcessingLog { get; set; }
    public string? ErrorMessage { get; set; }
    public int ProcessingProgress { get; set; } = 0;
    public string? Metadata { get; set; }
    public string? Language { get; set; }
    public DateTime? TranscribedAt { get; set; }
    public EmbeddingStatus EmbeddingStatus { get; set; } = EmbeddingStatus.None;
    public DateTime? EmbeddedAt { get; set; }
    public int EmbeddingProgress { get; set; } = 0;

    // Foreign Key
    public string UserId { get; set; } = string.Empty;

    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual ICollection<Job> Jobs { get; set; } = new List<Job>();
    public virtual ICollection<TranscriptSegment> TranscriptSegments { get; set; } = new List<TranscriptSegment>();
}