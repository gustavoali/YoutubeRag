using Hangfire;
using Microsoft.Extensions.Logging;
using YoutubeRag.Application.Interfaces.Services;
using YoutubeRag.Domain.Enums;
using YoutubeRag.Infrastructure.Jobs;

namespace YoutubeRag.Infrastructure.Services;

/// <summary>
/// Implementation of background job service using Hangfire
/// </summary>
public class HangfireJobService : IBackgroundJobService
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly ILogger<HangfireJobService> _logger;

    public HangfireJobService(
        IBackgroundJobClient backgroundJobClient,
        IRecurringJobManager recurringJobManager,
        ILogger<HangfireJobService> logger)
    {
        _backgroundJobClient = backgroundJobClient;
        _recurringJobManager = recurringJobManager;
        _logger = logger;
    }

    /// <inheritdoc/>
    public string EnqueueTranscriptionJob(string videoId, JobPriority priority = JobPriority.Normal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId, nameof(videoId));

        var queue = MapPriorityToQueue(priority);
        var jobId = _backgroundJobClient.Enqueue<TranscriptionBackgroundJob>(
            job => job.ExecuteAsync(videoId, CancellationToken.None));

        _logger.LogInformation("Enqueued transcription job for video: {VideoId}. HangfireJobId: {HangfireJobId}, Priority: {Priority}, Queue: {Queue}",
            videoId, jobId, priority, queue);

        return jobId;
    }

    /// <inheritdoc/>
    public string EnqueueEmbeddingJob(string videoId, JobPriority priority = JobPriority.Normal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId, nameof(videoId));

        var queue = MapPriorityToQueue(priority);
        var jobId = _backgroundJobClient.Enqueue<EmbeddingBackgroundJob>(
            job => job.ExecuteAsync(videoId, CancellationToken.None));

        _logger.LogInformation("Enqueued embedding job for video: {VideoId}. HangfireJobId: {HangfireJobId}, Priority: {Priority}, Queue: {Queue}",
            videoId, jobId, priority, queue);

        return jobId;
    }

    /// <inheritdoc/>
    public string EnqueueVideoProcessingJob(string videoId, JobPriority priority = JobPriority.Normal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId, nameof(videoId));

        var queue = MapPriorityToQueue(priority);
        var jobId = _backgroundJobClient.Enqueue<VideoProcessingBackgroundJob>(
            job => job.ExecuteAsync(videoId, CancellationToken.None));

        _logger.LogInformation("Enqueued video processing job for video: {VideoId}. HangfireJobId: {HangfireJobId}, Priority: {Priority}, Queue: {Queue}",
            videoId, jobId, priority, queue);

        return jobId;
    }

    /// <inheritdoc/>
    public string ScheduleTranscriptionJob(string videoId, TimeSpan delay)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId, nameof(videoId));

        var jobId = _backgroundJobClient.Schedule<TranscriptionBackgroundJob>(
            methodCall: job => job.ExecuteAsync(videoId, CancellationToken.None),
            delay: delay);

        var scheduledTime = DateTime.UtcNow.Add(delay);
        _logger.LogInformation("Scheduled transcription job for video: {VideoId}. HangfireJobId: {HangfireJobId}, ScheduledFor: {ScheduledTime}, Delay: {DelaySeconds}s",
            videoId, jobId, scheduledTime, delay.TotalSeconds);

        return jobId;
    }

    /// <inheritdoc/>
    public string ScheduleEmbeddingJob(string videoId, TimeSpan delay)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId, nameof(videoId));

        var jobId = _backgroundJobClient.Schedule<EmbeddingBackgroundJob>(
            methodCall: job => job.ExecuteAsync(videoId, CancellationToken.None),
            delay: delay);

        var scheduledTime = DateTime.UtcNow.Add(delay);
        _logger.LogInformation("Scheduled embedding job for video: {VideoId}. HangfireJobId: {HangfireJobId}, ScheduledFor: {ScheduledTime}, Delay: {DelaySeconds}s",
            videoId, jobId, scheduledTime, delay.TotalSeconds);

        return jobId;
    }

    /// <inheritdoc/>
    public bool DeleteJob(string jobId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId, nameof(jobId));

        _logger.LogInformation("Attempting to delete Hangfire job: {HangfireJobId}", jobId);
        var result = _backgroundJobClient.Delete(jobId);

        if (result)
        {
            _logger.LogInformation("Successfully deleted Hangfire job: {HangfireJobId}", jobId);
        }
        else
        {
            _logger.LogWarning("Failed to delete Hangfire job: {HangfireJobId}. It may have already been processed or does not exist",
                jobId);
        }

        return result;
    }

    /// <inheritdoc/>
    public void RecurringCleanupJob()
    {
        _recurringJobManager.AddOrUpdate<JobCleanupService>(
            "cleanup-old-jobs",
            service => service.CleanupOldJobsAsync(),
            Cron.Daily(2, 0)); // Run at 2:00 AM

        _recurringJobManager.AddOrUpdate<JobMonitoringService>(
            "monitor-stuck-jobs",
            service => service.CheckStuckJobsAsync(),
            Cron.MinuteInterval(30)); // Run every 30 minutes

        _logger.LogInformation("Configured recurring cleanup and monitoring jobs");
    }

    /// <summary>
    /// Maps JobPriority enum to Hangfire queue names
    /// NOTE: This method is currently used for logging purposes only.
    /// MySQL storage doesn't support specifying queues at enqueue time.
    /// Queue assignment is done via [Queue] attribute on job methods.
    /// </summary>
    /// <param name="priority">The job priority</param>
    /// <returns>The corresponding Hangfire queue name</returns>
    private static string MapPriorityToQueue(JobPriority priority)
    {
        return priority switch
        {
            JobPriority.Low => "low",
            JobPriority.Normal => "default",
            JobPriority.High => "default", // High priority still uses default queue but with priority
            JobPriority.Critical => "critical",
            _ => "default"
        };
    }
}
