using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Application.Interfaces.Services;

/// <summary>
/// Service interface for managing background jobs with Hangfire
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>
    /// Enqueue a transcription job for immediate processing
    /// </summary>
    /// <param name="videoId">The video ID to transcribe</param>
    /// <param name="priority">Job priority level</param>
    /// <returns>The Hangfire job ID</returns>
    string EnqueueTranscriptionJob(string videoId, JobPriority priority = JobPriority.Normal);

    /// <summary>
    /// Enqueue an embedding generation job for immediate processing
    /// </summary>
    /// <param name="videoId">The video ID to generate embeddings for</param>
    /// <param name="priority">Job priority level</param>
    /// <returns>The Hangfire job ID</returns>
    string EnqueueEmbeddingJob(string videoId, JobPriority priority = JobPriority.Normal);

    /// <summary>
    /// Enqueue a complete video processing job (transcription + embeddings)
    /// </summary>
    /// <param name="videoId">The video ID to process</param>
    /// <param name="priority">Job priority level</param>
    /// <returns>The Hangfire job ID</returns>
    string EnqueueVideoProcessingJob(string videoId, JobPriority priority = JobPriority.Normal);

    /// <summary>
    /// Schedule a transcription job to run after a delay
    /// </summary>
    /// <param name="videoId">The video ID to transcribe</param>
    /// <param name="delay">Time to wait before processing</param>
    /// <returns>The Hangfire job ID</returns>
    string ScheduleTranscriptionJob(string videoId, TimeSpan delay);

    /// <summary>
    /// Schedule an embedding generation job to run after a delay
    /// </summary>
    /// <param name="videoId">The video ID to generate embeddings for</param>
    /// <param name="delay">Time to wait before processing</param>
    /// <returns>The Hangfire job ID</returns>
    string ScheduleEmbeddingJob(string videoId, TimeSpan delay);

    /// <summary>
    /// Delete a scheduled or enqueued job
    /// </summary>
    /// <param name="jobId">The Hangfire job ID to delete</param>
    /// <returns>True if the job was deleted, false otherwise</returns>
    bool DeleteJob(string jobId);

    /// <summary>
    /// Setup recurring cleanup job
    /// </summary>
    void RecurringCleanupJob();
}