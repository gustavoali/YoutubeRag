using Hangfire;
using Hangfire.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Domain.Enums;
using YoutubeRag.Infrastructure.Data;

namespace YoutubeRag.Infrastructure.Services;

/// <summary>
/// Service for cleaning up old and orphaned jobs
/// </summary>
public class JobCleanupService
{
    private readonly ApplicationDbContext _context;
    private readonly IJobRepository _jobRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<JobCleanupService> _logger;
    private const int OLD_JOBS_DAYS_THRESHOLD = 30;
    private const int COMPLETED_JOBS_ARCHIVE_DAYS = 7;

    public JobCleanupService(
        ApplicationDbContext context,
        IJobRepository jobRepository,
        IUnitOfWork unitOfWork,
        ILogger<JobCleanupService> logger)
    {
        _context = context;
        _jobRepository = jobRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Clean up old jobs from the database
    /// </summary>
    public async Task CleanupOldJobsAsync()
    {
        _logger.LogInformation("Starting cleanup of old jobs");

        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-OLD_JOBS_DAYS_THRESHOLD);

            // Delete failed jobs older than threshold
            var failedJobsDeleted = await _context.Jobs
                .Where(j => j.Status == JobStatus.Failed && j.CreatedAt < cutoffDate)
                .ExecuteDeleteAsync();

            // Delete cancelled jobs older than threshold
            var cancelledJobsDeleted = await _context.Jobs
                .Where(j => j.Status == JobStatus.Cancelled && j.CreatedAt < cutoffDate)
                .ExecuteDeleteAsync();

            // Delete completed jobs older than threshold (keep recent for history)
            var veryOldCutoffDate = DateTime.UtcNow.AddDays(-90);
            var completedJobsDeleted = await _context.Jobs
                .Where(j => j.Status == JobStatus.Completed && j.CreatedAt < veryOldCutoffDate)
                .ExecuteDeleteAsync();

            _logger.LogInformation(
                "Cleaned up jobs: {Failed} failed, {Cancelled} cancelled, {Completed} very old completed",
                failedJobsDeleted, cancelledJobsDeleted, completedJobsDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during job cleanup");
        }
    }

    /// <summary>
    /// Archive completed jobs (move to archived status or separate table)
    /// </summary>
    public async Task ArchiveCompletedJobsAsync()
    {
        _logger.LogInformation("Starting archival of completed jobs");

        try
        {
            var archiveCutoffDate = DateTime.UtcNow.AddDays(-COMPLETED_JOBS_ARCHIVE_DAYS);

            // For now, we'll just mark old completed jobs with metadata
            // In a production system, you might move these to a separate archive table
            var jobsToArchive = await _context.Jobs
                .Where(j => j.Status == JobStatus.Completed
                    && j.CompletedAt != null
                    && j.CompletedAt < archiveCutoffDate
                    && !j.Metadata!.Contains("\"archived\":true"))
                .ToListAsync();

            foreach (var job in jobsToArchive)
            {
                // Add archived flag to metadata
                if (string.IsNullOrEmpty(job.Metadata))
                {
                    job.Metadata = "{\"archived\":true}";
                }
                else
                {
                    // Simple JSON manipulation - in production use a proper JSON library
                    job.Metadata = job.Metadata.TrimEnd('}') + ",\"archived\":true}";
                }

                job.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Archived {Count} completed jobs", jobsToArchive.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during job archival");
        }
    }

    /// <summary>
    /// Clean up orphaned Hangfire jobs (jobs in Hangfire but not in our database)
    /// </summary>
    public async Task CleanupOrphanedHangfireJobsAsync()
    {
        _logger.LogInformation("Starting cleanup of orphaned Hangfire jobs");

        try
        {
            using var connection = JobStorage.Current.GetConnection();
            var monitor = JobStorage.Current.GetMonitoringApi();

            // Get all Hangfire job IDs from various queues
            var hangfireJobIds = new HashSet<string>();

            // Check processing jobs
            var processingJobs = monitor.ProcessingJobs(0, int.MaxValue);
            foreach (var job in processingJobs)
            {
                hangfireJobIds.Add(job.Key);
            }

            // Check scheduled jobs
            var scheduledJobs = monitor.ScheduledJobs(0, int.MaxValue);
            foreach (var job in scheduledJobs)
            {
                hangfireJobIds.Add(job.Key);
            }

            // Check enqueued jobs
            foreach (var queue in monitor.Queues())
            {
                var enqueuedJobs = monitor.EnqueuedJobs(queue.Name, 0, int.MaxValue);
                foreach (var job in enqueuedJobs)
                {
                    hangfireJobIds.Add(job.Key);
                }
            }

            // Get all Hangfire job IDs from our database
            var databaseHangfireIds = await _context.Jobs
                .Where(j => !string.IsNullOrEmpty(j.HangfireJobId))
                .Select(j => j.HangfireJobId!)
                .ToListAsync();

            // Find orphaned Hangfire jobs (in Hangfire but not in our database)
            var orphanedJobIds = hangfireJobIds.Except(databaseHangfireIds).ToList();

            foreach (var orphanedId in orphanedJobIds)
            {
                try
                {
                    // Delete the orphaned job from Hangfire
                    BackgroundJob.Delete(orphanedId);
                    _logger.LogDebug("Deleted orphaned Hangfire job: {JobId}", orphanedId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete orphaned Hangfire job: {JobId}", orphanedId);
                }
            }

            if (orphanedJobIds.Any())
            {
                _logger.LogInformation("Cleaned up {Count} orphaned Hangfire jobs", orphanedJobIds.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during orphaned Hangfire job cleanup");
        }
    }
}
