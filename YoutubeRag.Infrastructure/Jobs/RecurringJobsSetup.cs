using Hangfire;
using YoutubeRag.Infrastructure.Services;

namespace YoutubeRag.Infrastructure.Jobs;

/// <summary>
/// Configuration for Hangfire recurring jobs
/// </summary>
public static class RecurringJobsSetup
{
    /// <summary>
    /// Configure all recurring jobs for the application
    /// </summary>
    /// <param name="recurringJobManager">The Hangfire recurring job manager</param>
    public static void ConfigureRecurringJobs(IRecurringJobManager recurringJobManager)
    {
        // Clean up old jobs every day at 2 AM
        recurringJobManager.AddOrUpdate<JobCleanupService>(
            "cleanup-old-jobs",
            service => service.CleanupOldJobsAsync(),
            Cron.Daily(2, 0)); // 2:00 AM

        // Check for stuck jobs every 30 minutes
        recurringJobManager.AddOrUpdate<JobMonitoringService>(
            "monitor-stuck-jobs",
            service => service.CheckStuckJobsAsync(),
            Cron.MinuteInterval(30));

        // Clean up orphaned Hangfire jobs every 6 hours
        recurringJobManager.AddOrUpdate<JobCleanupService>(
            "cleanup-orphaned-hangfire-jobs",
            service => service.CleanupOrphanedHangfireJobsAsync(),
            Cron.HourInterval(6));

        // Archive completed jobs weekly (Sunday at 3 AM)
        recurringJobManager.AddOrUpdate<JobCleanupService>(
            "archive-completed-jobs",
            service => service.ArchiveCompletedJobsAsync(),
            Cron.Weekly(DayOfWeek.Sunday, 3, 0)); // Sunday at 3:00 AM

        // Clean up unused Whisper models daily at 3 AM
        recurringJobManager.AddOrUpdate<WhisperModelCleanupJob>(
            "cleanup-whisper-models",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Daily(3, 0)); // 3:00 AM
    }
}