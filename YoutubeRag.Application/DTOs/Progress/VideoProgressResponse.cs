using System.Text.Json.Serialization;

namespace YoutubeRag.Application.DTOs.Progress;

/// <summary>
/// Response DTO for video processing progress information
/// </summary>
public class VideoProgressResponse
{
    /// <summary>
    /// The unique identifier of the video
    /// </summary>
    [JsonPropertyName("video_id")]
    public string VideoId { get; set; } = string.Empty;

    /// <summary>
    /// The unique identifier of the job
    /// </summary>
    [JsonPropertyName("job_id")]
    public string? JobId { get; set; }

    /// <summary>
    /// Current processing status of the job
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Current progress percentage (0-100)
    /// </summary>
    [JsonPropertyName("progress_percentage")]
    public int ProgressPercentage { get; set; }

    /// <summary>
    /// Current processing stage description
    /// </summary>
    [JsonPropertyName("current_stage")]
    public string? CurrentStage { get; set; }

    /// <summary>
    /// Hangfire background job identifier
    /// </summary>
    [JsonPropertyName("hangfire_job_id")]
    public string? HangfireJobId { get; set; }

    /// <summary>
    /// Timestamp when the job was started
    /// </summary>
    [JsonPropertyName("started_at")]
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Estimated completion timestamp
    /// </summary>
    [JsonPropertyName("estimated_completion")]
    public DateTime? EstimatedCompletion { get; set; }

    /// <summary>
    /// Error message if the job failed
    /// </summary>
    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Timestamp when the job was last updated
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
