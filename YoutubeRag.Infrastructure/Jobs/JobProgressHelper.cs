using Microsoft.Extensions.Logging;
using YoutubeRag.Domain.Entities;

namespace YoutubeRag.Infrastructure.Jobs;

/// <summary>
/// Helper class for job progress management and validation
/// GAP-P2-3: Ensures progress alignment and monotonic progress updates
/// </summary>
public static class JobProgressHelper
{
    /// <summary>
    /// Updates job progress ensuring it never goes backward (monotonic progress)
    /// </summary>
    /// <param name="job">The job to update</param>
    /// <param name="newProgress">The new progress value (0-100)</param>
    /// <param name="logger">Logger for warnings</param>
    /// <returns>True if progress was updated, false if progress would go backward</returns>
    public static bool UpdateProgressMonotonic(Job job, double newProgress, ILogger logger)
    {
        if (newProgress < 0 || newProgress > 100)
        {
            logger.LogWarning(
                "Invalid progress value {Progress} for job {JobId}. Progress must be between 0 and 100.",
                newProgress,
                job.Id);
            return false;
        }

        // Ensure monotonic progress - never go backward
        if (newProgress < job.Progress)
        {
            logger.LogDebug(
                "Skipping progress update for job {JobId}. Current: {Current}%, New: {New}%. Progress cannot go backward.",
                job.Id,
                job.Progress,
                newProgress);
            return false;
        }

        // Progress is valid and moving forward
        job.Progress = (int)Math.Round(newProgress);
        return true;
    }

    /// <summary>
    /// Validates that stage progress values sum to approximately 100%
    /// </summary>
    /// <param name="job">The job to validate</param>
    /// <param name="logger">Logger for warnings</param>
    /// <param name="tolerance">Acceptable deviation from 100% (default: 1.0%)</param>
    public static void ValidateStageProgress(Job job, ILogger logger, double tolerance = 1.0)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(job.StageProgressJson))
            {
                return;
            }

            var stageProgress = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, double>>(
                job.StageProgressJson);

            if (stageProgress == null || stageProgress.Count == 0)
            {
                return;
            }

            // Calculate total progress from all stages
            var total = stageProgress.Values.Sum();
            var deviation = Math.Abs(total - 100.0);

            if (deviation > tolerance)
            {
                logger.LogWarning(
                    "Stage progress inconsistency for job {JobId}. Total: {Total}%, Expected: 100%. Deviation: {Deviation}%",
                    job.Id,
                    total,
                    deviation);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating stage progress for job {JobId}", job.Id);
        }
    }

    /// <summary>
    /// Ensures stage progress is within valid range (0-100)
    /// </summary>
    /// <param name="stageProgress">The stage progress value</param>
    /// <returns>Clamped progress value between 0 and 100</returns>
    public static double ClampProgress(double stageProgress)
    {
        return Math.Max(0, Math.Min(100, stageProgress));
    }

    /// <summary>
    /// Calculates overall job progress ensuring it's monotonic
    /// </summary>
    /// <param name="job">The job</param>
    /// <param name="newStageProgress">New progress for the current stage</param>
    /// <param name="logger">Logger</param>
    /// <returns>New overall progress value</returns>
    public static double CalculateAndValidateProgress(Job job, double newStageProgress, ILogger logger)
    {
        // Clamp stage progress to valid range
        newStageProgress = ClampProgress(newStageProgress);

        // Calculate new overall progress
        var oldProgress = job.Progress;
        var newOverallProgress = job.CalculateOverallProgress();

        // Ensure progress is monotonic
        if (newOverallProgress < oldProgress)
        {
            logger.LogDebug(
                "Calculated progress {New}% is less than current {Current}% for job {JobId}. Using current value.",
                newOverallProgress,
                oldProgress,
                job.Id);
            return oldProgress;
        }

        return newOverallProgress;
    }
}
