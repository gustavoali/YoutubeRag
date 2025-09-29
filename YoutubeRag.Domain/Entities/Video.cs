using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Domain.Entities;

public class Video : BaseEntity
{
    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    [Column(TypeName = "TEXT")]
    public string? Description { get; set; }

    [StringLength(50)]
    public string? YoutubeId { get; set; }

    [StringLength(500)]
    public string? YoutubeUrl { get; set; }

    [StringLength(500)]
    public string? OriginalUrl { get; set; }

    [StringLength(500)]
    public string? ThumbnailUrl { get; set; }

    public TimeSpan? Duration { get; set; }
    public int? ViewCount { get; set; }
    public int? LikeCount { get; set; }

    [Required]
    public VideoStatus Status { get; set; } = VideoStatus.Pending;

    [StringLength(500)]
    public string? FilePath { get; set; }

    [StringLength(500)]
    public string? AudioPath { get; set; }

    [Column(TypeName = "TEXT")]
    public string? ProcessingLog { get; set; }

    [Column(TypeName = "TEXT")]
    public string? ErrorMessage { get; set; }

    public int ProcessingProgress { get; set; } = 0;

    [Column(TypeName = "JSON")]
    public string? Metadata { get; set; }

    // Foreign Key
    [Required]
    public string UserId { get; set; } = string.Empty;

    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual ICollection<Job> Jobs { get; set; } = new List<Job>();
    public virtual ICollection<TranscriptSegment> TranscriptSegments { get; set; } = new List<TranscriptSegment>();
}