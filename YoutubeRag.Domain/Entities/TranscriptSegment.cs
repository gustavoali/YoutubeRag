using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YoutubeRag.Domain.Entities;

public class TranscriptSegment : BaseEntity
{
    [Required]
    public string VideoId { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "TEXT")]
    public string Text { get; set; } = string.Empty;

    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public int SegmentIndex { get; set; }

    [Column(TypeName = "JSON")]
    public string? Embedding { get; set; }

    public double? Confidence { get; set; }

    [StringLength(10)]
    public string? Language { get; set; }

    // Navigation Properties
    public virtual Video Video { get; set; } = null!;
}