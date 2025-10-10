using System.ComponentModel.DataAnnotations;

namespace YoutubeRag.Application.DTOs.Job;

/// <summary>
/// Data transfer object for creating a new job
/// </summary>
public record CreateJobDto
{
    /// <summary>
    /// Gets the job type (e.g., "video_processing", "transcription", "embedding")
    /// </summary>
    [Required(ErrorMessage = "Job type is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Job type must be between 1 and 100 characters")]
    public string JobType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the video ID to process (optional, depending on job type)
    /// </summary>
    public string? VideoId { get; init; }

    /// <summary>
    /// Gets the job parameters as JSON
    /// </summary>
    public string? Parameters { get; init; }

    /// <summary>
    /// Gets custom metadata as JSON
    /// </summary>
    public string? Metadata { get; init; }

    /// <summary>
    /// Gets the maximum number of retries for this job
    /// </summary>
    [Range(0, 10, ErrorMessage = "Max retries must be between 0 and 10")]
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// Gets the priority of the job (higher number = higher priority)
    /// </summary>
    [Range(0, 100, ErrorMessage = "Priority must be between 0 and 100")]
    public int Priority { get; init; } = 50;

    /// <summary>
    /// Gets whether to start the job immediately
    /// </summary>
    public bool StartImmediately { get; init; } = true;
}