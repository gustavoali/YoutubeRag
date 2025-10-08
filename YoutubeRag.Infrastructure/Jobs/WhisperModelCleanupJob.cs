using Hangfire;
using Microsoft.Extensions.Logging;
using YoutubeRag.Application.Interfaces;

namespace YoutubeRag.Infrastructure.Jobs;

/// <summary>
/// Hangfire recurring job that cleans up unused Whisper models.
/// Runs daily to remove models that haven't been used within the configured retention period.
/// Always keeps the 'tiny' model as a fallback.
/// </summary>
public class WhisperModelCleanupJob
{
    private readonly IWhisperModelDownloadService _downloadService;
    private readonly ILogger<WhisperModelCleanupJob> _logger;

    public WhisperModelCleanupJob(
        IWhisperModelDownloadService downloadService,
        ILogger<WhisperModelCleanupJob> logger)
    {
        _downloadService = downloadService ?? throw new ArgumentNullException(nameof(downloadService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the cleanup job.
    /// Called by Hangfire on a recurring schedule.
    /// </summary>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Whisper model cleanup job");

        try
        {
            var deletedCount = await _downloadService.CleanupUnusedModelsAsync(cancellationToken);

            _logger.LogInformation(
                "Whisper model cleanup job completed successfully. Deleted {Count} models",
                deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Whisper model cleanup job failed");
            throw;
        }
    }

    /// <summary>
    /// Registers this job with Hangfire to run on a recurring schedule.
    /// Called during application startup.
    /// </summary>
    public static void Register()
    {
        // Run daily at 3 AM
        RecurringJob.AddOrUpdate<WhisperModelCleanupJob>(
            "whisper-model-cleanup",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Daily(3));
    }
}
