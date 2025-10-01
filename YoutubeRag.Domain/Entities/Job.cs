using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Domain.Entities;

public class Job : BaseEntity
{
    [Required]
    [StringLength(100)]
    public string JobType { get; set; } = string.Empty;

    [Required]
    public JobStatus Status { get; set; } = JobStatus.Pending;

    [StringLength(500)]
    public string? StatusMessage { get; set; }

    public int Progress { get; set; } = 0;

    [Column(TypeName = "TEXT")]
    public string? Result { get; set; }

    [Column(TypeName = "TEXT")]
    public string? ErrorMessage { get; set; }

    [Column(TypeName = "JSON")]
    public string? Parameters { get; set; }

    [Column(TypeName = "JSON")]
    public string? Metadata { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;

    [StringLength(255)]
    public string? WorkerId { get; set; }

    // Foreign Keys
    [Required]
    public string UserId { get; set; } = string.Empty;

    public string? VideoId { get; set; }

    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual Video? Video { get; set; }
}