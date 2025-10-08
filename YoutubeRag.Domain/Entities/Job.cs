using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Domain.Entities;

public class Job : BaseEntity
{
    public JobType Type { get; set; } = JobType.VideoProcessing;
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public string? StatusMessage { get; set; }
    public int Progress { get; set; } = 0;
    public string? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Parameters { get; set; }
    public string? Metadata { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
    public int Priority { get; set; } = 1;
    public string? WorkerId { get; set; }
    public string? HangfireJobId { get; set; }

    // Foreign Keys
    public string UserId { get; set; } = string.Empty;

    public string? VideoId { get; set; }

    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual Video? Video { get; set; }
}