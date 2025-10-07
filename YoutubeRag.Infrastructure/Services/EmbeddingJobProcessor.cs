using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YoutubeRag.Application.DTOs.Progress;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Application.Interfaces.Services;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;
using YoutubeRag.Infrastructure.Data;

namespace YoutubeRag.Infrastructure.Services;

/// <summary>
/// Service for processing embedding generation jobs
/// </summary>
public class EmbeddingJobProcessor
{
    private readonly ILogger<EmbeddingJobProcessor> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IEmbeddingService _embeddingService;
    private readonly ISegmentationService _segmentationService;
    private readonly ITranscriptSegmentRepository _segmentRepository;
    private readonly IProgressNotificationService _progressNotificationService;
    private const int BATCH_SIZE = 32;
    private const int MAX_RETRY_COUNT = 3;

    public EmbeddingJobProcessor(
        ILogger<EmbeddingJobProcessor> logger,
        ApplicationDbContext context,
        IEmbeddingService embeddingService,
        ISegmentationService segmentationService,
        ITranscriptSegmentRepository segmentRepository,
        IProgressNotificationService progressNotificationService)
    {
        _logger = logger;
        _context = context;
        _embeddingService = embeddingService;
        _segmentationService = segmentationService;
        _segmentRepository = segmentRepository;
        _progressNotificationService = progressNotificationService;
    }

    /// <summary>
    /// Processes an embedding job for a video
    /// </summary>
    /// <param name="videoId">The video ID to process</param>
    /// <param name="jobId">Optional job ID for progress notifications</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful, false otherwise</returns>
    public async Task<bool> ProcessEmbeddingJobAsync(string videoId, string? jobId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId, nameof(videoId));

        _logger.LogInformation("Starting embedding job for video {VideoId}", videoId);

        try
        {
            // Get the video
            var video = await _context.Videos
                .Include(v => v.TranscriptSegments)
                .FirstOrDefaultAsync(v => v.Id == videoId, cancellationToken);

            if (video == null)
            {
                _logger.LogError("Video {VideoId} not found", videoId);
                return false;
            }

            // Update video status to InProgress
            await UpdateVideoEmbeddingStatusAsync(video, EmbeddingStatus.InProgress, cancellationToken);

            // Check if model is available
            var isModelAvailable = await _embeddingService.IsModelAvailableAsync();
            if (!isModelAvailable)
            {
                _logger.LogError("Embedding model is not available");
                await UpdateVideoEmbeddingStatusAsync(video, EmbeddingStatus.Failed, cancellationToken);
                return false;
            }

            // Get segments without embeddings
            var segmentsWithoutEmbeddings = await _segmentRepository.GetSegmentsWithoutEmbeddingsAsync(
                videoId, cancellationToken);

            if (!segmentsWithoutEmbeddings.Any())
            {
                _logger.LogInformation("No segments to process for video {VideoId}", videoId);
                await UpdateVideoEmbeddingStatusAsync(video, EmbeddingStatus.Completed, cancellationToken);
                return true;
            }

            _logger.LogInformation("Processing {Count} segments for video {VideoId}",
                segmentsWithoutEmbeddings.Count, videoId);

            // Notify: Loading segments
            if (!string.IsNullOrEmpty(jobId))
            {
                await _progressNotificationService.NotifyJobProgressAsync(jobId, new JobProgressDto
                {
                    JobId = jobId,
                    VideoId = videoId,
                    JobType = "EmbeddingGeneration",
                    Status = "Running",
                    Progress = 10,
                    CurrentStage = "Loading segments",
                    StatusMessage = $"Loaded {segmentsWithoutEmbeddings.Count} segments for processing",
                    UpdatedAt = DateTime.UtcNow
                });
            }

            // Process segments in batches
            var totalSegments = segmentsWithoutEmbeddings.Count;
            var processedCount = 0;
            var failedCount = 0;

            foreach (var batch in segmentsWithoutEmbeddings.Chunk(BATCH_SIZE))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batchResult = await ProcessBatchWithRetryAsync(batch, cancellationToken);
                processedCount += batchResult.SuccessCount;
                failedCount += batchResult.FailureCount;

                // Update progress
                var progress = (int)((processedCount + failedCount) * 100 / totalSegments);
                await UpdateVideoProgressAsync(video, progress, cancellationToken);

                // Notify: Batch processed
                if (!string.IsNullOrEmpty(jobId))
                {
                    // Progress from 10% to 90% during batch processing
                    var jobProgress = 10 + (int)(progress * 0.8);
                    await _progressNotificationService.NotifyJobProgressAsync(jobId, new JobProgressDto
                    {
                        JobId = jobId,
                        VideoId = videoId,
                        JobType = "EmbeddingGeneration",
                        Status = "Running",
                        Progress = jobProgress,
                        CurrentStage = "Generating embeddings",
                        StatusMessage = $"Processed {processedCount + failedCount}/{totalSegments} segments",
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                _logger.LogDebug("Processed batch: {Success} succeeded, {Failed} failed",
                    batchResult.SuccessCount, batchResult.FailureCount);
            }

            // Determine final status
            EmbeddingStatus finalStatus;
            if (failedCount == 0)
            {
                finalStatus = EmbeddingStatus.Completed;
                _logger.LogInformation("Successfully generated all embeddings for video {VideoId}", videoId);
            }
            else if (processedCount > 0)
            {
                finalStatus = EmbeddingStatus.Partial;
                _logger.LogWarning("Partially generated embeddings for video {VideoId}: {Success} succeeded, {Failed} failed",
                    videoId, processedCount, failedCount);
            }
            else
            {
                finalStatus = EmbeddingStatus.Failed;
                _logger.LogError("Failed to generate any embeddings for video {VideoId}", videoId);
            }

            await UpdateVideoEmbeddingStatusAsync(video, finalStatus, cancellationToken);
            return finalStatus != EmbeddingStatus.Failed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing embedding job for video {VideoId}", videoId);

            // Try to update status to failed
            try
            {
                var video = await _context.Videos.FindAsync(new object[] { videoId }, cancellationToken);
                if (video != null)
                {
                    await UpdateVideoEmbeddingStatusAsync(video, EmbeddingStatus.Failed, cancellationToken);
                }
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx, "Failed to update video status after error");
            }

            return false;
        }
    }

    /// <summary>
    /// Processes a job by job ID
    /// </summary>
    /// <param name="jobId">The job ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ProcessJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId, nameof(jobId));

        _logger.LogInformation("Processing embedding job {JobId}", jobId);

        var job = await _context.Jobs
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

        if (job == null)
        {
            _logger.LogError("Job {JobId} not found", jobId);
            return;
        }

        if (job.Type != JobType.EmbeddingGeneration)
        {
            _logger.LogError("Job {JobId} is not an embedding job", jobId);
            return;
        }

        try
        {
            job.Status = JobStatus.Running;
            job.StartedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            // Notify: Job started
            await _progressNotificationService.NotifyJobProgressAsync(job.Id, new JobProgressDto
            {
                JobId = job.Id,
                VideoId = job.VideoId,
                JobType = "EmbeddingGeneration",
                Status = "Running",
                Progress = 0,
                CurrentStage = "Starting embedding generation",
                StatusMessage = "Initializing embedding job",
                UpdatedAt = DateTime.UtcNow
            });

            var success = await ProcessEmbeddingJobAsync(job.VideoId, job.Id, cancellationToken);

            job.Status = success ? JobStatus.Completed : JobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            job.Progress = 100;

            if (success)
            {
                // Notify: Job completed
                await _progressNotificationService.NotifyJobCompletedAsync(
                    job.Id,
                    job.VideoId,
                    "Completed");
            }
            else
            {
                job.ErrorMessage = "Failed to generate embeddings";

                // Notify: Job failed
                await _progressNotificationService.NotifyJobFailedAsync(
                    job.Id,
                    job.VideoId,
                    job.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing job {JobId}", jobId);
            job.Status = JobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            job.ErrorMessage = ex.Message;

            // Notify: Job failed
            await _progressNotificationService.NotifyJobFailedAsync(
                job.Id,
                job.VideoId,
                ex.Message);
        }
        finally
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Creates an embedding job for a video
    /// </summary>
    /// <param name="videoId">The video ID</param>
    /// <param name="userId">The user ID</param>
    /// <param name="priority">Job priority</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created job</returns>
    public async Task<Job> CreateEmbeddingJobAsync(
        string videoId,
        string userId,
        JobPriority priority = JobPriority.Normal,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId, nameof(videoId));
        ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = videoId,
            UserId = userId,
            Type = JobType.EmbeddingGeneration,
            Status = JobStatus.Pending,
            Priority = (int)priority,
            Progress = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Jobs.Add(job);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created embedding job {JobId} for video {VideoId}", job.Id, videoId);

        return job;
    }

    /// <summary>
    /// Processes a batch of segments with retry logic
    /// </summary>
    private async Task<BatchResult> ProcessBatchWithRetryAsync(
        TranscriptSegment[] batch,
        CancellationToken cancellationToken)
    {
        var segmentTexts = batch
            .Select(s => (s.Id, s.Text))
            .ToList();

        for (int attempt = 1; attempt <= MAX_RETRY_COUNT; attempt++)
        {
            try
            {
                var embeddings = await _embeddingService.GenerateEmbeddingsAsync(
                    segmentTexts, cancellationToken);

                if (embeddings.Any())
                {
                    await _segmentRepository.UpdateEmbeddingsAsync(embeddings, cancellationToken);
                    return new BatchResult { SuccessCount = embeddings.Count, FailureCount = batch.Length - embeddings.Count };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Attempt {Attempt} failed for batch processing", attempt);

                if (attempt == MAX_RETRY_COUNT)
                {
                    _logger.LogError(ex, "Max retries reached for batch processing");
                    return new BatchResult { SuccessCount = 0, FailureCount = batch.Length };
                }

                // Exponential backoff
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
            }
        }

        return new BatchResult { SuccessCount = 0, FailureCount = batch.Length };
    }

    /// <summary>
    /// Updates video embedding status
    /// </summary>
    private async Task UpdateVideoEmbeddingStatusAsync(
        Video video,
        EmbeddingStatus status,
        CancellationToken cancellationToken)
    {
        video.EmbeddingStatus = status;
        video.UpdatedAt = DateTime.UtcNow;

        if (status == EmbeddingStatus.Completed || status == EmbeddingStatus.Partial)
        {
            video.EmbeddedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Updates video embedding progress
    /// </summary>
    private async Task UpdateVideoProgressAsync(
        Video video,
        int progress,
        CancellationToken cancellationToken)
    {
        video.EmbeddingProgress = progress;
        video.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Result of batch processing
    /// </summary>
    private class BatchResult
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
    }
}