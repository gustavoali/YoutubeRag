using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Infrastructure.Jobs;

/// <summary>
/// Background job that cleans up old completed jobs from the database
/// to prevent unbounded growth of the Jobs table
/// </summary>
public class JobCleanupJob
{
    private readonly IJobRepository _jobRepository;
    private readonly ILogger<JobCleanupJob> _logger;
    private readonly CleanupOptions _options;

    public JobCleanupJob(
        IJobRepository jobRepository,
        ILogger<JobCleanupJob> logger,
        IOptions<CleanupOptions> options)
    {
        _jobRepository = jobRepository;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Executes the job cleanup process
    /// Deletes completed jobs older than the configured retention period
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting job cleanup process");

        try
        {
            // Calculate cutoff date based on retention days
            var retentionDays = _options.JobRetentionDays;
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

            _logger.LogInformation("Cleaning up completed jobs older than {CutoffDate} (retention: {Days} days)",
                cutoffDate, retentionDays);

            // Get old completed jobs
            var oldJobs = await _jobRepository.GetByDateRangeAsync(
                DateTime.MinValue,
                cutoffDate);

            // Filter to only completed jobs
            var jobsToDelete = oldJobs
                .Where(j => j.Status == JobStatus.Completed && j.CompletedAt.HasValue && j.CompletedAt.Value < cutoffDate)
                .ToList();

            if (jobsToDelete.Count == 0)
            {
                _logger.LogInformation("No old completed jobs found for cleanup");
                return;
            }

            _logger.LogInformation("Found {Count} old completed jobs to delete", jobsToDelete.Count);

            // Delete jobs one by one (in case bulk delete is not supported)
            int deletedCount = 0;
            foreach (var job in jobsToDelete)
            {
                try
                {
                    await _jobRepository.DeleteAsync(job.Id);
                    deletedCount++;

                    if (deletedCount % 10 == 0)
                    {
                        _logger.LogDebug("Deleted {Count}/{Total} jobs", deletedCount, jobsToDelete.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete job {JobId}", job.Id);
                    // Continue with other jobs
                }

                // Check for cancellation
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Job cleanup cancelled after deleting {Count} jobs", deletedCount);
                    break;
                }
            }

            _logger.LogInformation(
                "Job cleanup completed. Deleted {DeletedCount} of {TotalCount} old completed jobs",
                deletedCount,
                jobsToDelete.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job cleanup process failed: {Error}", ex.Message);
            throw;
        }
    }
}

/// <summary>
/// Configuration options for cleanup jobs
/// </summary>
public class CleanupOptions
{
    public const string SectionName = "Cleanup";

    /// <summary>
    /// Number of days to retain completed jobs (default: 30)
    /// </summary>
    public int JobRetentionDays { get; set; } = 30;

    /// <summary>
    /// Number of days to retain read notifications (default: 60)
    /// </summary>
    public int NotificationRetentionDays { get; set; } = 60;

    /// <summary>
    /// Time window in minutes to consider notifications as duplicates (default: 5)
    /// </summary>
    public int NotificationDeduplicationMinutes { get; set; } = 5;
}
