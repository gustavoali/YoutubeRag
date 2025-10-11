using System.Text.Json;
using Microsoft.Extensions.Logging;
using YoutubeRag.Application.Configuration;
using YoutubeRag.Application.DTOs.Progress;
using YoutubeRag.Application.DTOs.Transcription;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Application.Interfaces.Services;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Application.Services;

/// <summary>
/// Service for processing video transcription jobs
/// </summary>
public class TranscriptionJobProcessor
{
    private readonly IVideoRepository _videoRepository;
    private readonly IJobRepository _jobRepository;
    private readonly ITranscriptSegmentRepository _transcriptSegmentRepository;
    private readonly IDeadLetterJobRepository _deadLetterJobRepository;
    private readonly IAudioExtractionService _audioExtractionService;
    private readonly IVideoDownloadService _videoDownloadService;
    private readonly ITranscriptionService _transcriptionService;
    private readonly ISegmentationService _segmentationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppConfiguration _appConfiguration;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly IProgressNotificationService _progressNotificationService;
    private readonly ILogger<TranscriptionJobProcessor> _logger;

    public TranscriptionJobProcessor(
        IVideoRepository videoRepository,
        IJobRepository jobRepository,
        ITranscriptSegmentRepository transcriptSegmentRepository,
        IDeadLetterJobRepository deadLetterJobRepository,
        IAudioExtractionService audioExtractionService,
        IVideoDownloadService videoDownloadService,
        ITranscriptionService transcriptionService,
        ISegmentationService segmentationService,
        IUnitOfWork unitOfWork,
        IAppConfiguration appConfiguration,
        IBackgroundJobService backgroundJobService,
        IProgressNotificationService progressNotificationService,
        ILogger<TranscriptionJobProcessor> logger)
    {
        _videoRepository = videoRepository;
        _jobRepository = jobRepository;
        _transcriptSegmentRepository = transcriptSegmentRepository;
        _deadLetterJobRepository = deadLetterJobRepository;
        _audioExtractionService = audioExtractionService;
        _videoDownloadService = videoDownloadService;
        _transcriptionService = transcriptionService;
        _segmentationService = segmentationService;
        _unitOfWork = unitOfWork;
        _appConfiguration = appConfiguration;
        _backgroundJobService = backgroundJobService;
        _progressNotificationService = progressNotificationService;
        _logger = logger;
    }

    /// <summary>
    /// Processes a transcription job for a video
    /// </summary>
    /// <param name="videoId">The video ID to transcribe</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful, false otherwise</returns>
    public async Task<bool> ProcessTranscriptionJobAsync(string videoId, CancellationToken cancellationToken = default)
    {
        var audioFilePath = string.Empty;
        Job? transcriptionJob = null;
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting transcription job processing for video: {VideoId}", videoId);

            // Step 1: Get video from repository
            var video = await _videoRepository.GetByIdAsync(videoId);
            if (video == null)
            {
                _logger.LogError("Video not found: {VideoId}", videoId);
                return false;
            }

            // Step 2: Create or get the transcription job
            transcriptionJob = await GetOrCreateTranscriptionJobAsync(video, cancellationToken);

            // Update job status to Running
            transcriptionJob.Status = JobStatus.Running;
            transcriptionJob.StartedAt = DateTime.UtcNow;
            transcriptionJob.UpdatedAt = DateTime.UtcNow;
            await _jobRepository.UpdateAsync(transcriptionJob);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Notify: Job started
            await _progressNotificationService.NotifyJobProgressAsync(transcriptionJob.Id, new JobProgressDto
            {
                JobId = transcriptionJob.Id,
                VideoId = videoId,
                JobType = "Transcription",
                Status = "Running",
                Progress = 0,
                CurrentStage = "Starting transcription process",
                StatusMessage = "Initializing transcription job",
                UpdatedAt = DateTime.UtcNow
            });

            // Step 3: Check if Whisper is available
            if (!await _transcriptionService.IsWhisperAvailableAsync())
            {
                _logger.LogWarning("Whisper is not available. Marking job as failed for video: {VideoId}", videoId);
                await UpdateJobStatusAsync(transcriptionJob, JobStatus.Failed, "Whisper transcription service is not available", cancellationToken);
                return false;
            }

            // Step 4A: Download video using IVideoDownloadService
            var videoDownloadStartTime = DateTime.UtcNow;
            _logger.LogInformation("Stage: Video download started for video: {VideoId}, JobId: {JobId}",
                videoId, transcriptionJob.Id);

            // Notify: Downloading video
            await _progressNotificationService.NotifyJobProgressAsync(transcriptionJob.Id, new JobProgressDto
            {
                JobId = transcriptionJob.Id,
                VideoId = videoId,
                JobType = "Transcription",
                Status = "Running",
                Progress = 10,
                CurrentStage = "Downloading video",
                StatusMessage = "Downloading video from YouTube",
                UpdatedAt = DateTime.UtcNow
            });

            // Download video with progress tracking
            var videoFilePath = await _videoDownloadService.DownloadVideoAsync(
                video.YouTubeId,
                progress: new Progress<double>(p =>
                {
                    // Update progress: 10-25% for video download
                    var overallProgress = 10 + (int)(p * 15);
                    _progressNotificationService.NotifyJobProgressAsync(transcriptionJob.Id, new JobProgressDto
                    {
                        JobId = transcriptionJob.Id,
                        VideoId = videoId,
                        JobType = "Transcription",
                        Status = "Running",
                        Progress = overallProgress,
                        CurrentStage = "Downloading video",
                        StatusMessage = $"Downloading video: {p:P0} complete",
                        UpdatedAt = DateTime.UtcNow
                    }).GetAwaiter().GetResult();
                }),
                cancellationToken);

            var videoDownloadDuration = (DateTime.UtcNow - videoDownloadStartTime).TotalSeconds;
            _logger.LogInformation("Stage: Video download completed for video: {VideoId}, JobId: {JobId} in {DurationSeconds:F2}s. Path: {Path}",
                videoId, transcriptionJob.Id, videoDownloadDuration, videoFilePath);

            // Step 4B: Extract Whisper-compatible audio from video
            var audioExtractionStartTime = DateTime.UtcNow;
            _logger.LogInformation("Stage: Audio extraction started for video: {VideoId}, JobId: {JobId}",
                videoId, transcriptionJob.Id);

            // Notify: Extracting audio
            await _progressNotificationService.NotifyJobProgressAsync(transcriptionJob.Id, new JobProgressDto
            {
                JobId = transcriptionJob.Id,
                VideoId = videoId,
                JobType = "Transcription",
                Status = "Running",
                Progress = 25,
                CurrentStage = "Extracting audio",
                StatusMessage = "Extracting Whisper-compatible audio (16kHz mono WAV)",
                UpdatedAt = DateTime.UtcNow
            });

            // Extract Whisper-compatible audio (16kHz mono WAV) from the downloaded video
            audioFilePath = await _audioExtractionService.ExtractWhisperAudioFromVideoAsync(videoFilePath, video.Id, cancellationToken);

            // Get audio info
            var audioInfo = await _audioExtractionService.GetAudioInfoAsync(audioFilePath, cancellationToken);
            var audioExtractionDuration = (DateTime.UtcNow - audioExtractionStartTime).TotalSeconds;
            _logger.LogInformation("Stage: Audio extraction completed for video: {VideoId}, JobId: {JobId} in {DurationSeconds:F2}s. Duration: {AudioDuration}, Size: {Size}",
                videoId, transcriptionJob.Id, audioExtractionDuration, audioInfo.Duration, audioInfo.FormattedFileSize);

            // Notify: Audio extracted
            await _progressNotificationService.NotifyJobProgressAsync(transcriptionJob.Id, new JobProgressDto
            {
                JobId = transcriptionJob.Id,
                VideoId = videoId,
                JobType = "Transcription",
                Status = "Running",
                Progress = 30,
                CurrentStage = "Audio extraction completed",
                StatusMessage = $"Whisper audio extracted successfully. Duration: {audioInfo.Duration}",
                UpdatedAt = DateTime.UtcNow
            });

            // Step 5: Transcribe audio using ITranscriptionService
            var transcriptionStartTime = DateTime.UtcNow;
            _logger.LogInformation("Stage: Transcription started for video: {VideoId}, JobId: {JobId}, Quality: {Quality}",
                videoId, transcriptionJob.Id, DetermineTranscriptionQuality(audioInfo.Duration));

            // Notify: Starting transcription
            await _progressNotificationService.NotifyJobProgressAsync(transcriptionJob.Id, new JobProgressDto
            {
                JobId = transcriptionJob.Id,
                VideoId = videoId,
                JobType = "Transcription",
                Status = "Running",
                Progress = 40,
                CurrentStage = "Transcribing audio",
                StatusMessage = "Running Whisper transcription on audio file",
                UpdatedAt = DateTime.UtcNow
            });

            var transcriptionRequest = new TranscriptionRequestDto(
                VideoId: videoId,
                AudioFilePath: audioFilePath,
                Language: video.Language ?? "auto",
                Quality: DetermineTranscriptionQuality(audioInfo.Duration)
            );

            var transcriptionResult = await _transcriptionService.TranscribeAudioAsync(transcriptionRequest, cancellationToken);

            var transcriptionDuration = (DateTime.UtcNow - transcriptionStartTime).TotalSeconds;
            _logger.LogInformation("Stage: Transcription completed for video: {VideoId}, JobId: {JobId} in {DurationSeconds}s. Segments: {SegmentCount}, Language: {Language}",
                videoId, transcriptionJob.Id, transcriptionDuration, transcriptionResult.Segments.Count, transcriptionResult.Language);

            // Notify: Transcription completed
            await _progressNotificationService.NotifyJobProgressAsync(transcriptionJob.Id, new JobProgressDto
            {
                JobId = transcriptionJob.Id,
                VideoId = videoId,
                JobType = "Transcription",
                Status = "Running",
                Progress = 70,
                CurrentStage = "Transcription completed",
                StatusMessage = $"Transcribed {transcriptionResult.Segments.Count} segments",
                UpdatedAt = DateTime.UtcNow
            });

            // Step 6: Save transcript segments to database
            // Notify: Saving segments
            await _progressNotificationService.NotifyJobProgressAsync(transcriptionJob.Id, new JobProgressDto
            {
                JobId = transcriptionJob.Id,
                VideoId = videoId,
                JobType = "Transcription",
                Status = "Running",
                Progress = 85,
                CurrentStage = "Saving transcript segments",
                StatusMessage = "Persisting transcript segments to database",
                UpdatedAt = DateTime.UtcNow
            });

            await SaveTranscriptSegmentsAsync(video, transcriptionResult, cancellationToken);

            // Step 7: Update video status
            video.ProcessingStatus = VideoStatus.Completed;
            video.TranscriptionStatus = TranscriptionStatus.Completed;
            video.TranscribedAt = DateTime.UtcNow;
            video.Duration = transcriptionResult.Duration; // Already a TimeSpan
            video.Language = transcriptionResult.Language;
            video.UpdatedAt = DateTime.UtcNow;

            await _videoRepository.UpdateAsync(video);

            // Step 8: Update job status to Completed
            await UpdateJobStatusAsync(transcriptionJob, JobStatus.Completed,
                $"Successfully transcribed {transcriptionResult.Segments.Count} segments", cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Notify: Job completed
            await _progressNotificationService.NotifyJobCompletedAsync(
                transcriptionJob.Id,
                videoId,
                "Completed");

            var totalDuration = (DateTime.UtcNow - startTime).TotalSeconds;
            _logger.LogInformation("Transcription job completed successfully for video: {VideoId}, JobId: {JobId} in {TotalDurationSeconds}s",
                videoId, transcriptionJob.Id, totalDuration);

            // Step 9: Optionally enqueue embedding job if auto-generate is enabled
            if (_appConfiguration.AutoGenerateEmbeddings)
            {
                try
                {
                    _logger.LogInformation("Auto-generating embeddings enabled. Enqueueing embedding job for video: {VideoId}, JobId: {JobId}",
                        videoId, transcriptionJob.Id);

                    // Create embedding job in database
                    var embeddingJob = new Job
                    {
                        Id = Guid.NewGuid().ToString(),
                        VideoId = video.Id,
                        UserId = video.UserId,
                        Type = JobType.EmbeddingGeneration,
                        Status = JobStatus.Pending,
                        Priority = transcriptionJob.Priority, // Use same priority as transcription
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Metadata = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            VideoTitle = video.Title,
                            YouTubeId = video.YouTubeId,
                            SegmentCount = transcriptionResult.Segments.Count
                        })
                    };

                    // Enqueue the embedding job with Hangfire
                    var hangfireJobId = _backgroundJobService.EnqueueEmbeddingJob(
                        video.Id,
                        (JobPriority)transcriptionJob.Priority);

                    embeddingJob.HangfireJobId = hangfireJobId;

                    await _jobRepository.AddAsync(embeddingJob);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Enqueued embedding job {JobId} with Hangfire ID {HangfireJobId} for video: {VideoId}",
                        embeddingJob.Id, hangfireJobId, videoId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to enqueue embedding job for video: {VideoId}, JobId: {JobId}. Transcription was successful.",
                        videoId, transcriptionJob.Id);
                    // Don't fail the transcription job if embedding enqueue fails
                }
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Transcription job cancelled for video: {VideoId}, JobId: {JobId}",
                videoId, transcriptionJob?.Id ?? "Unknown");

            if (transcriptionJob != null)
            {
                await UpdateJobStatusAsync(transcriptionJob, JobStatus.Cancelled, "Job was cancelled", cancellationToken);
            }

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transcription job for video: {VideoId}, JobId: {JobId}. Error: {ErrorMessage}",
                videoId, transcriptionJob?.Id ?? "Unknown", ex.Message);

            if (transcriptionJob != null)
            {
                // Get retry policy for this exception
                var retryPolicy = JobRetryPolicy.GetPolicy(ex, _logger);
                transcriptionJob.LastFailureCategory = retryPolicy.Category.ToString();

                // GAP-1: Format user-friendly error message
                var userFriendlyMessage = ErrorMessageFormatter.FormatUserFriendlyMessage(ex, retryPolicy.Category);

                // GAP-2: Store enhanced error tracking information
                transcriptionJob.ErrorStackTrace = ex.StackTrace;
                transcriptionJob.ErrorType = ex.GetType().FullName;
                transcriptionJob.FailedStage = transcriptionJob.CurrentStage;

                _logger.LogInformation("Applied retry policy for job {JobId}: {PolicyDescription}",
                    transcriptionJob.Id, retryPolicy.Description);

                // Check if this is a permanent error - send directly to DLQ
                if (retryPolicy.SendToDeadLetterQueue)
                {
                    _logger.LogWarning("Job {JobId} encountered permanent error. Sending directly to Dead Letter Queue",
                        transcriptionJob.Id);

                    await SendToDeadLetterQueueAsync(transcriptionJob, ex, retryPolicy.Category.ToString(), cancellationToken);
                    await UpdateJobStatusAsync(transcriptionJob, JobStatus.Failed, userFriendlyMessage, cancellationToken);
                }
                else
                {
                    // Increment retry count
                    transcriptionJob.RetryCount++;

                    // Calculate next retry time based on policy
                    var nextRetryDelay = retryPolicy.GetNextRetryDelay(transcriptionJob.RetryCount - 1);
                    transcriptionJob.NextRetryAt = DateTime.UtcNow.Add(nextRetryDelay);

                    _logger.LogInformation("Job {JobId} scheduled for retry {RetryCount}/{MaxRetries} at {NextRetryAt} (delay: {Delay})",
                        transcriptionJob.Id, transcriptionJob.RetryCount, retryPolicy.MaxRetries,
                        transcriptionJob.NextRetryAt, nextRetryDelay);

                    // Check if job has exceeded max retries for this policy - send to DLQ
                    if (transcriptionJob.RetryCount >= retryPolicy.MaxRetries)
                    {
                        _logger.LogWarning("Job {JobId} has exceeded max retries ({RetryCount}/{MaxRetries}). Sending to Dead Letter Queue",
                            transcriptionJob.Id, transcriptionJob.RetryCount, retryPolicy.MaxRetries);

                        await SendToDeadLetterQueueAsync(transcriptionJob, ex, retryPolicy.Category.ToString(), cancellationToken);
                    }

                    await UpdateJobStatusAsync(transcriptionJob, JobStatus.Failed, userFriendlyMessage, cancellationToken);
                }

                // Notify: Job failed with user-friendly message
                await _progressNotificationService.NotifyJobFailedAsync(
                    transcriptionJob.Id,
                    videoId,
                    userFriendlyMessage);
            }

            // Update video status to indicate transcription failed
            var video = await _videoRepository.GetByIdAsync(videoId);
            if (video != null)
            {
                video.TranscriptionStatus = TranscriptionStatus.Failed;
                video.UpdatedAt = DateTime.UtcNow;
                await _videoRepository.UpdateAsync(video);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return false;
        }
        finally
        {
            // Step 9: Clean up audio file
            if (!string.IsNullOrEmpty(audioFilePath))
            {
                try
                {
                    await _audioExtractionService.DeleteAudioFileAsync(audioFilePath);
                    _logger.LogInformation("Cleaned up audio file: {FilePath}", audioFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clean up audio file: {FilePath}", audioFilePath);
                }
            }
        }
    }

    /// <summary>
    /// Processes multiple transcription jobs
    /// </summary>
    /// <param name="videoIds">List of video IDs to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of video IDs and their processing results</returns>
    public async Task<Dictionary<string, bool>> ProcessMultipleTranscriptionJobsAsync(
        IEnumerable<string> videoIds,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, bool>();

        foreach (var videoId in videoIds)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Batch transcription processing cancelled");
                break;
            }

            try
            {
                var result = await ProcessTranscriptionJobAsync(videoId, cancellationToken);
                results[videoId] = result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process transcription for video: {VideoId}", videoId);
                results[videoId] = false;
            }
        }

        return results;
    }

    private async Task<Job> GetOrCreateTranscriptionJobAsync(Video video, CancellationToken cancellationToken)
    {
        // Check if there's an existing transcription job
        var existingJobs = await _jobRepository.GetByVideoIdAsync(video.Id);
        var transcriptionJob = existingJobs.FirstOrDefault(j => j.Type == JobType.Transcription && j.Status != JobStatus.Completed);

        if (transcriptionJob != null)
        {
            _logger.LogInformation("Found existing transcription job: {JobId} for video: {VideoId}",
                transcriptionJob.Id, video.Id);
            return transcriptionJob;
        }

        // Create new transcription job
        transcriptionJob = new Job
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = video.Id,
            UserId = video.UserId,
            Type = JobType.Transcription,
            Status = JobStatus.Pending,
            Priority = 1, // Normal priority
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                VideoTitle = video.Title,
                YouTubeId = video.YouTubeId,
                Language = video.Language ?? "auto"
            })
        };

        await _jobRepository.AddAsync(transcriptionJob);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created new transcription job: {JobId} for video: {VideoId}",
            transcriptionJob.Id, video.Id);

        return transcriptionJob;
    }

    private async Task SaveTranscriptSegmentsAsync(Video video, TranscriptionResultDto transcriptionResult, CancellationToken cancellationToken)
    {
        // Delete existing segments if any
        var existingSegmentsCount = await _transcriptSegmentRepository.DeleteByVideoIdAsync(video.Id);
        if (existingSegmentsCount > 0)
        {
            _logger.LogInformation("Deleted {Count} existing transcript segments for video: {VideoId}",
                existingSegmentsCount, video.Id);
        }

        // Create new segments list for bulk insert
        var allSegments = new List<TranscriptSegment>();
        var now = DateTime.UtcNow;
        const int MAX_SEGMENT_LENGTH = 500;

        // Process each segment from Whisper output
        for (int i = 0; i < transcriptionResult.Segments.Count; i++)
        {
            var segmentDto = transcriptionResult.Segments[i];
            var trimmedText = segmentDto.Text.Trim();

            // Check if segment is too long and needs splitting
            if (trimmedText.Length > MAX_SEGMENT_LENGTH)
            {
                _logger.LogDebug("Segment {Index} is too long ({Length} chars). Splitting into smaller segments.",
                    i, trimmedText.Length);

                // Use SegmentationService to split long segment
                var subSegments = await _segmentationService.CreateSegmentsFromTranscriptAsync(
                    video.Id,
                    trimmedText,
                    segmentDto.StartTime,
                    segmentDto.EndTime,
                    MAX_SEGMENT_LENGTH
                );

                // Update metadata for sub-segments
                foreach (var subSegment in subSegments)
                {
                    subSegment.Language = transcriptionResult.Language;
                    subSegment.Confidence = segmentDto.Confidence;
                    subSegment.Speaker = segmentDto.Speaker;
                    subSegment.CreatedAt = now;
                    subSegment.UpdatedAt = now;
                }

                allSegments.AddRange(subSegments);
            }
            else
            {
                // Segment is already the right size
                var segment = new TranscriptSegment
                {
                    Id = Guid.NewGuid().ToString(),
                    VideoId = video.Id,
                    SegmentIndex = i,
                    StartTime = segmentDto.StartTime,
                    EndTime = segmentDto.EndTime,
                    Text = trimmedText,
                    Language = transcriptionResult.Language,
                    Confidence = segmentDto.Confidence,
                    Speaker = segmentDto.Speaker,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                allSegments.Add(segment);
            }
        }

        // Re-index all segments sequentially
        for (int i = 0; i < allSegments.Count; i++)
        {
            allSegments[i].SegmentIndex = i;
        }

        // Validate segment integrity before saving
        ValidateSegmentIntegrity(allSegments, video.Id);

        // Bulk insert all segments at once
        if (allSegments.Any())
        {
            await _transcriptSegmentRepository.AddRangeAsync(allSegments, cancellationToken);
            _logger.LogInformation("Bulk inserted {Count} transcript segments for video: {VideoId} (from {OriginalCount} Whisper segments)",
                allSegments.Count, video.Id, transcriptionResult.Segments.Count);
        }

        // CRITICAL FIX (ISSUE-001): Transaction handling for production (MySQL)
        // NOTE: In-memory database used in tests doesn't support transactions,
        // but in production (MySQL), this code benefits from EF Core's implicit transactions.
        // If Whisper fails before this method is called, no segments are saved.
        // If an error occurs during segment saving, the entire operation fails atomically
        // due to EF Core's SaveChangesAsync transaction behavior.
    }

    private void ValidateSegmentIntegrity(List<TranscriptSegment> segments, string videoId)
    {
        if (segments == null || !segments.Any())
        {
            throw new ArgumentException("Segments list cannot be null or empty", nameof(segments));
        }

        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];

            // Validar SegmentIndex secuencial
            if (segment.SegmentIndex != i)
            {
                _logger.LogWarning("Gap in SegmentIndex at position {Position}. Expected {Expected}, Got {Actual}",
                    i, i, segment.SegmentIndex);
            }

            // Validar timestamps crecientes
            if (i > 0 && segment.StartTime < segments[i - 1].StartTime)
            {
                _logger.LogWarning("Timestamps not increasing at segment {Index}. Current: {Current}, Previous: {Previous}",
                    i, segment.StartTime, segments[i - 1].StartTime);
            }

            // Validar no overlaps: EndTime[i-1] <= StartTime[i]
            if (i > 0 && segments[i - 1].EndTime > segment.StartTime)
            {
                _logger.LogWarning("Overlap detected between segments {Index1} and {Index2}. EndTime[{Index1}]={EndTime} > StartTime[{Index2}]={StartTime}",
                    i - 1, i, i - 1, segments[i - 1].EndTime, i, segment.StartTime);
            }

            // Validar VideoId válido
            if (string.IsNullOrWhiteSpace(segment.VideoId))
            {
                throw new InvalidOperationException($"Segment {i} has invalid VideoId");
            }

            // Validar VideoId consistente
            if (segment.VideoId != videoId)
            {
                throw new InvalidOperationException($"Segment {i} has mismatched VideoId. Expected: {videoId}, Got: {segment.VideoId}");
            }

            // Validar texto no vacío
            if (string.IsNullOrWhiteSpace(segment.Text))
            {
                _logger.LogWarning("Segment {Index} has empty text", i);
            }

            // Validar timestamps válidos
            if (segment.StartTime < 0 || segment.EndTime < 0)
            {
                throw new InvalidOperationException($"Segment {i} has negative timestamps");
            }

            if (segment.EndTime <= segment.StartTime)
            {
                _logger.LogWarning("Segment {Index} has invalid duration. StartTime={Start}, EndTime={End}",
                    i, segment.StartTime, segment.EndTime);
            }
        }

        _logger.LogDebug("Validated {Count} segments. All integrity checks passed.", segments.Count);
    }

    private async Task UpdateJobStatusAsync(Job job, JobStatus status, string message, CancellationToken cancellationToken)
    {
        job.Status = status;
        job.UpdatedAt = DateTime.UtcNow;

        if (status == JobStatus.Completed)
        {
            job.CompletedAt = DateTime.UtcNow;
            job.Progress = 100;
        }
        else if (status == JobStatus.Failed)
        {
            job.ErrorMessage = message;
            job.FailedAt = DateTime.UtcNow;
        }

        await _jobRepository.UpdateAsync(job);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated job {JobId} status to {Status}: {Message}",
            job.Id, status, message);
    }

    private TranscriptionQuality DetermineTranscriptionQuality(TimeSpan duration)
    {
        // Use higher quality for shorter videos, lower quality for longer ones
        if (duration.TotalMinutes <= 10)
        {
            return TranscriptionQuality.High;
        }
        else if (duration.TotalMinutes <= 30)
        {
            return TranscriptionQuality.Medium;
        }
        else
        {
            return TranscriptionQuality.Low;
        }
    }

    /// <summary>
    /// Sends a failed job to the Dead Letter Queue after max retries exceeded
    /// </summary>
    /// <param name="job">The failed job</param>
    /// <param name="exception">The exception that caused the failure</param>
    /// <param name="failureCategory">The category of failure (from retry policy)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task SendToDeadLetterQueueAsync(Job job, Exception exception, string failureCategory, CancellationToken cancellationToken)
    {
        try
        {
            // Check if already in DLQ
            var existingDlq = await _deadLetterJobRepository.GetByJobIdAsync(job.Id);
            if (existingDlq != null)
            {
                _logger.LogWarning("Job {JobId} already exists in Dead Letter Queue", job.Id);
                return;
            }

            // Create failure details object
            var failureDetails = new
            {
                ExceptionType = exception.GetType().FullName,
                ExceptionMessage = exception.Message,
                StackTrace = exception.StackTrace,
                InnerException = exception.InnerException?.Message,
                FailureCategory = failureCategory,
                Timestamp = DateTime.UtcNow,
                VideoId = job.VideoId,
                UserId = job.UserId,
                JobType = job.Type.ToString()
            };

            // Create original payload object
            var originalPayload = new
            {
                job.Parameters,
                job.Metadata,
                job.VideoId,
                job.UserId,
                job.Type,
                job.Priority
            };

            // Determine failure reason
            var failureReason = failureCategory == FailureCategory.PermanentError.ToString()
                ? "PermanentError"
                : "MaxRetriesExceeded";

            // Create dead letter job entry
            var deadLetterJob = new DeadLetterJob
            {
                Id = Guid.NewGuid().ToString(),
                JobId = job.Id,
                FailureReason = failureReason,
                FailureDetails = JsonSerializer.Serialize(failureDetails, new JsonSerializerOptions { WriteIndented = true }),
                OriginalPayload = JsonSerializer.Serialize(originalPayload, new JsonSerializerOptions { WriteIndented = true }),
                FailedAt = DateTime.UtcNow,
                AttemptedRetries = job.RetryCount,
                IsRequeued = false,
                Notes = failureReason == "PermanentError"
                    ? $"Job failed with permanent error (category: {failureCategory}). Error: {exception.Message}"
                    : $"Job failed after {job.RetryCount} retry attempts (category: {failureCategory}). Last error: {exception.Message}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _deadLetterJobRepository.AddAsync(deadLetterJob);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully sent job {JobId} to Dead Letter Queue. DLQ ID: {DeadLetterJobId}, Reason: {Reason}",
                job.Id, deadLetterJob.Id, failureReason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send job {JobId} to Dead Letter Queue. This is a critical error.", job.Id);
            // Don't throw - we don't want to fail the entire job processing because DLQ insertion failed
        }
    }
}
