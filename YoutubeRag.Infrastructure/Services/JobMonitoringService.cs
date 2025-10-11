using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Domain.Enums;
using YoutubeRag.Infrastructure.Data;

namespace YoutubeRag.Infrastructure.Services;

/// <summary>
/// Service for monitoring and fixing stuck jobs
/// </summary>
public class JobMonitoringService
{
    private readonly ApplicationDbContext _context;
    private readonly IJobRepository _jobRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<JobMonitoringService> _logger;
    private const int STUCK_JOB_HOURS_THRESHOLD = 2;
    private const int MAX_JOB_RUNTIME_HOURS = 6;

    public JobMonitoringService(
        ApplicationDbContext context,
        IJobRepository jobRepository,
        IUnitOfWork unitOfWork,
        ILogger<JobMonitoringService> logger)
    {
        _context = context;
        _jobRepository = jobRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Check for stuck jobs and mark them as failed
    /// </summary>
    public async Task CheckStuckJobsAsync()
    {
        _logger.LogInformation("Starting check for stuck jobs");

        try
        {
            var stuckJobThreshold = DateTime.UtcNow.AddHours(-STUCK_JOB_HOURS_THRESHOLD);
            var maxRuntimeThreshold = DateTime.UtcNow.AddHours(-MAX_JOB_RUNTIME_HOURS);

            // Find jobs that are stuck in Running state
            var stuckRunningJobs = await _context.Jobs
                .Where(j => j.Status == JobStatus.Running
                    && j.StartedAt != null
                    && j.StartedAt < stuckJobThreshold)
                .ToListAsync();

            foreach (var job in stuckRunningJobs)
            {
                _logger.LogWarning(
                    "Found stuck running job {JobId} for video {VideoId}, started at {StartedAt}",
                    job.Id, job.VideoId, job.StartedAt);

                // Check if job has been running too long
                if (job.StartedAt < maxRuntimeThreshold)
                {
                    job.Status = JobStatus.Failed;
                    job.ErrorMessage = $"Job exceeded maximum runtime of {MAX_JOB_RUNTIME_HOURS} hours";
                    job.FailedAt = DateTime.UtcNow;
                    job.UpdatedAt = DateTime.UtcNow;

                    // Try to cancel the Hangfire job if it exists
                    if (!string.IsNullOrEmpty(job.HangfireJobId))
                    {
                        try
                        {
                            BackgroundJob.Delete(job.HangfireJobId);
                            _logger.LogInformation("Cancelled stuck Hangfire job {HangfireJobId}", job.HangfireJobId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to cancel Hangfire job {HangfireJobId}", job.HangfireJobId);
                        }
                    }
                }
                else
                {
                    // Job is stuck but not too old, just log it
                    _logger.LogWarning(
                        "Job {JobId} appears stuck but within max runtime. Will check again later.",
                        job.Id);
                }
            }

            // Find jobs that are stuck in Pending state for too long
            var oldPendingThreshold = DateTime.UtcNow.AddHours(-24);
            var stuckPendingJobs = await _context.Jobs
                .Where(j => j.Status == JobStatus.Pending
                    && j.CreatedAt < oldPendingThreshold)
                .ToListAsync();

            foreach (var job in stuckPendingJobs)
            {
                _logger.LogWarning(
                    "Found old pending job {JobId} for video {VideoId}, created at {CreatedAt}",
                    job.Id, job.VideoId, job.CreatedAt);

                // Check if there's a Hangfire job for this
                if (!string.IsNullOrEmpty(job.HangfireJobId))
                {
                    // Check if the Hangfire job still exists
                    var hangfireJobExists = await CheckHangfireJobExistsAsync(job.HangfireJobId);
                    if (!hangfireJobExists)
                    {
                        // Hangfire job is gone, mark as failed
                        job.Status = JobStatus.Failed;
                        job.ErrorMessage = "Hangfire job was lost or deleted";
                        job.FailedAt = DateTime.UtcNow;
                        job.UpdatedAt = DateTime.UtcNow;

                        _logger.LogWarning(
                            "Marking job {JobId} as failed - Hangfire job {HangfireJobId} no longer exists",
                            job.Id, job.HangfireJobId);
                    }
                }
                else
                {
                    // No Hangfire job ID and it's old - likely never got queued
                    job.Status = JobStatus.Failed;
                    job.ErrorMessage = "Job was never queued for processing";
                    job.FailedAt = DateTime.UtcNow;
                    job.UpdatedAt = DateTime.UtcNow;

                    _logger.LogWarning(
                        "Marking job {JobId} as failed - never queued to Hangfire",
                        job.Id);
                }
            }

            await _context.SaveChangesAsync();

            var totalStuckJobs = stuckRunningJobs.Count + stuckPendingJobs.Count;
            if (totalStuckJobs > 0)
            {
                _logger.LogInformation(
                    "Processed {Total} stuck jobs: {Running} running, {Pending} pending",
                    totalStuckJobs, stuckRunningJobs.Count, stuckPendingJobs.Count);
            }

            // Also check for jobs with too many retries
            await CheckFailedRetriesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during stuck job monitoring");
        }
    }

    /// <summary>
    /// Check for jobs that have exceeded their retry limit
    /// </summary>
    private async Task CheckFailedRetriesAsync()
    {
        try
        {
            var jobsWithExceededRetries = await _context.Jobs
                .Where(j => j.Status == JobStatus.Running || j.Status == JobStatus.Pending)
                .Where(j => j.RetryCount >= j.MaxRetries)
                .ToListAsync();

            foreach (var job in jobsWithExceededRetries)
            {
                _logger.LogWarning(
                    "Job {JobId} has exceeded max retries ({RetryCount}/{MaxRetries})",
                    job.Id, job.RetryCount, job.MaxRetries);

                job.Status = JobStatus.Failed;
                job.ErrorMessage = $"Exceeded maximum retry attempts ({job.MaxRetries})";
                job.FailedAt = DateTime.UtcNow;
                job.UpdatedAt = DateTime.UtcNow;
            }

            if (jobsWithExceededRetries.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation(
                    "Marked {Count} jobs as failed due to exceeded retries",
                    jobsWithExceededRetries.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for failed retries");
        }
    }

    /// <summary>
    /// Check if a Hangfire job exists
    /// </summary>
    private async Task<bool> CheckHangfireJobExistsAsync(string hangfireJobId)
    {
        try
        {
            var connection = JobStorage.Current.GetConnection();
            var jobData = connection.GetJobData(hangfireJobId);
            return jobData != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking Hangfire job existence for {JobId}", hangfireJobId);
            return false;
        }
    }
}
