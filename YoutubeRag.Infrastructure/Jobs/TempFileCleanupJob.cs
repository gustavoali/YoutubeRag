using Hangfire;
using Microsoft.Extensions.Logging;
using YoutubeRag.Application.Configuration;
using YoutubeRag.Application.Interfaces;

namespace YoutubeRag.Infrastructure.Jobs;

/// <summary>
/// Hangfire recurring job for cleaning up old temporary files
/// Executes daily at 3:00 AM via Hangfire scheduler
/// </summary>
public class TempFileCleanupJob
{
    private readonly ITempFileManagementService _tempFileService;
    private readonly IAppConfiguration _appConfiguration;
    private readonly ILogger<TempFileCleanupJob> _logger;

    public TempFileCleanupJob(
        ITempFileManagementService tempFileService,
        IAppConfiguration appConfiguration,
        ILogger<TempFileCleanupJob> logger)
    {
        _tempFileService = tempFileService;
        _appConfiguration = appConfiguration;
        _logger = logger;
    }

    /// <summary>
    /// Executes cleanup of old temporary files
    /// Files older than configured hours (default: 24) will be deleted
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting temp file cleanup job");

            // Get cleanup age from configuration (default 24 hours)
            // This allows flexibility to adjust retention period via appsettings.json
            var cleanupAfterHours = _appConfiguration.CleanupAfterHours ?? 24;

            _logger.LogInformation(
                "Cleanup configuration: Deleting files older than {Hours} hours",
                cleanupAfterHours);

            // Get storage stats before cleanup
            var statsBefore = await _tempFileService.GetStorageStatsAsync();

            _logger.LogInformation(
                "Storage stats before cleanup: {TotalFiles} files, {TotalSize}, " +
                "{VideoDirectories} directories, {AvailableSpace} available",
                statsBefore.TotalFiles,
                statsBefore.FormattedTotalSize,
                statsBefore.VideoDirectoryCount,
                statsBefore.FormattedAvailableSpace);

            // Execute cleanup
            var deletedCount = await _tempFileService.CleanupOldFilesAsync(
                cleanupAfterHours,
                cancellationToken);

            // Get storage stats after cleanup
            var statsAfter = await _tempFileService.GetStorageStatsAsync();

            var freedSpace = statsBefore.TotalSizeBytes - statsAfter.TotalSizeBytes;
            var duration = (DateTime.UtcNow - startTime).TotalSeconds;

            _logger.LogInformation(
                "Temp file cleanup completed successfully in {DurationSeconds:F2}s. " +
                "Deleted {DeletedCount} files, Freed {FreedMB:F2}MB. " +
                "Current stats: {CurrentFiles} files, {CurrentSize}, {AvailableSpace} available",
                duration,
                deletedCount,
                freedSpace / (1024.0 * 1024.0),
                statsAfter.TotalFiles,
                statsAfter.FormattedTotalSize,
                statsAfter.FormattedAvailableSpace);

            // Log warning if disk space is running low
            var minDiskSpaceGB = _appConfiguration.MinDiskSpaceGB ?? 5; // Default 5GB minimum
            var availableSpaceGB = statsAfter.AvailableDiskSpaceBytes / (1024.0 * 1024.0 * 1024.0);

            if (availableSpaceGB < minDiskSpaceGB)
            {
                _logger.LogWarning(
                    "Low disk space warning: {AvailableGB:F2}GB available, " +
                    "minimum recommended: {MinGB}GB. Consider increasing cleanup frequency or storage capacity.",
                    availableSpaceGB, minDiskSpaceGB);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Temp file cleanup job was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            _logger.LogError(ex,
                "Temp file cleanup job failed after {DurationSeconds:F2}s. Error: {ErrorMessage}",
                duration, ex.Message);

            // Re-throw to let Hangfire register this as a failed job
            // This will trigger automatic retry based on [AutomaticRetry] attribute
            throw;
        }
    }
}
