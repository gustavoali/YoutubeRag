using YoutubeRag.Domain.Enums;
using System.Text.Json;

namespace YoutubeRag.Domain.Entities;

public class Job : BaseEntity
{
    public JobType Type { get; set; } = JobType.VideoProcessing;
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public string? StatusMessage { get; set; }
    public int Progress { get; set; } = 0;
    public PipelineStage CurrentStage { get; set; } = PipelineStage.None;
    public string? StageProgressJson { get; set; }
    public string? Result { get; set; }
    public string? ErrorMessage { get; set; }

    // Enhanced error tracking (GAP-2)
    /// <summary>
    /// Full stack trace of the last error (for debugging)
    /// </summary>
    public string? ErrorStackTrace { get; set; }

    /// <summary>
    /// Type of the exception that caused the error (e.g., "HttpRequestException")
    /// </summary>
    public string? ErrorType { get; set; }

    /// <summary>
    /// Pipeline stage where the job failed
    /// </summary>
    public PipelineStage? FailedStage { get; set; }
    public string? Parameters { get; set; }
    public string? Metadata { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
    public DateTime? NextRetryAt { get; set; }
    public string? LastFailureCategory { get; set; }
    public int Priority { get; set; } = 1;
    public string? WorkerId { get; set; }
    public string? HangfireJobId { get; set; }

    // Foreign Keys
    public string UserId { get; set; } = string.Empty;

    public string? VideoId { get; set; }

    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual Video? Video { get; set; }

    // Helper methods for stage progress tracking
    /// <summary>
    /// Gets the progress for each pipeline stage
    /// </summary>
    public Dictionary<PipelineStage, double> GetStageProgress()
    {
        if (string.IsNullOrWhiteSpace(StageProgressJson))
        {
            return new Dictionary<PipelineStage, double>();
        }

        try
        {
            var progress = JsonSerializer.Deserialize<Dictionary<string, double>>(StageProgressJson);
            if (progress == null) return new Dictionary<PipelineStage, double>();

            return progress.ToDictionary(
                kvp => Enum.Parse<PipelineStage>(kvp.Key),
                kvp => kvp.Value
            );
        }
        catch
        {
            return new Dictionary<PipelineStage, double>();
        }
    }

    /// <summary>
    /// Sets the progress for a specific pipeline stage
    /// </summary>
    public void SetStageProgress(PipelineStage stage, double progress)
    {
        var stageProgress = GetStageProgress();
        stageProgress[stage] = Math.Clamp(progress, 0, 100);

        StageProgressJson = JsonSerializer.Serialize(stageProgress.ToDictionary(
            kvp => kvp.Key.ToString(),
            kvp => kvp.Value
        ));
    }

    /// <summary>
    /// Calculates overall progress based on stage weights
    /// </summary>
    public int CalculateOverallProgress()
    {
        var stageProgress = GetStageProgress();
        if (stageProgress.Count == 0) return 0;

        // Stage weights (total = 100)
        var weights = new Dictionary<PipelineStage, double>
        {
            { PipelineStage.Download, 20 },
            { PipelineStage.AudioExtraction, 15 },
            { PipelineStage.Transcription, 50 },
            { PipelineStage.Segmentation, 15 }
        };

        double totalProgress = 0;
        foreach (var kvp in stageProgress)
        {
            if (weights.TryGetValue(kvp.Key, out var weight))
            {
                totalProgress += (kvp.Value / 100.0) * weight;
            }
        }

        return (int)Math.Round(totalProgress);
    }
}