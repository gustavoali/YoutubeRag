using Microsoft.Extensions.Logging;
using YoutubeRag.Application.Interfaces.Services;
using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Infrastructure.Services.Mock;

/// <summary>
/// Mock implementation of IBackgroundJobService for testing
/// </summary>
public class MockBackgroundJobService : IBackgroundJobService
{
    private readonly ILogger<MockBackgroundJobService> _logger;
    private int _jobIdCounter = 1;

    public MockBackgroundJobService(ILogger<MockBackgroundJobService> logger)
    {
        _logger = logger;
    }

    public string EnqueueTranscriptionJob(string videoId, JobPriority priority = JobPriority.Normal)
    {
        var jobId = $"mock-transcription-job-{_jobIdCounter++}";
        _logger.LogInformation("Mock: Enqueued transcription job {JobId} for video {VideoId} with priority {Priority}",
            jobId, videoId, priority);
        return jobId;
    }

    public string EnqueueEmbeddingJob(string videoId, JobPriority priority = JobPriority.Normal)
    {
        var jobId = $"mock-embedding-job-{_jobIdCounter++}";
        _logger.LogInformation("Mock: Enqueued embedding job {JobId} for video {VideoId} with priority {Priority}",
            jobId, videoId, priority);
        return jobId;
    }

    public string EnqueueVideoProcessingJob(string videoId, JobPriority priority = JobPriority.Normal)
    {
        var jobId = $"mock-video-processing-job-{_jobIdCounter++}";
        _logger.LogInformation("Mock: Enqueued video processing job {JobId} for video {VideoId} with priority {Priority}",
            jobId, videoId, priority);
        return jobId;
    }

    public string ScheduleTranscriptionJob(string videoId, TimeSpan delay)
    {
        var jobId = $"mock-scheduled-transcription-job-{_jobIdCounter++}";
        _logger.LogInformation("Mock: Scheduled transcription job {JobId} for video {VideoId} with delay {Delay}",
            jobId, videoId, delay);
        return jobId;
    }

    public string ScheduleEmbeddingJob(string videoId, TimeSpan delay)
    {
        var jobId = $"mock-scheduled-embedding-job-{_jobIdCounter++}";
        _logger.LogInformation("Mock: Scheduled embedding job {JobId} for video {VideoId} with delay {Delay}",
            jobId, videoId, delay);
        return jobId;
    }

    public bool DeleteJob(string jobId)
    {
        _logger.LogInformation("Mock: Deleted job {JobId}", jobId);
        return true;
    }

    public void RecurringCleanupJob()
    {
        _logger.LogInformation("Mock: Setup recurring cleanup job");
    }
}
