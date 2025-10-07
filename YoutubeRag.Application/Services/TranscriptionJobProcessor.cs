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
    private readonly IAudioExtractionService _audioExtractionService;
    private readonly ITranscriptionService _transcriptionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppConfiguration _appConfiguration;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly IProgressNotificationService _progressNotificationService;
    private readonly ILogger<TranscriptionJobProcessor> _logger;

    public TranscriptionJobProcessor(
        IVideoRepository videoRepository,
        IJobRepository jobRepository,
        ITranscriptSegmentRepository transcriptSegmentRepository,
        IAudioExtractionService audioExtractionService,
        ITranscriptionService transcriptionService,
        IUnitOfWork unitOfWork,
        IAppConfiguration appConfiguration,
        IBackgroundJobService backgroundJobService,
        IProgressNotificationService progressNotificationService,
        ILogger<TranscriptionJobProcessor> logger)
    {
        _videoRepository = videoRepository;
        _jobRepository = jobRepository;
        _transcriptSegmentRepository = transcriptSegmentRepository;
        _audioExtractionService = audioExtractionService;
        _transcriptionService = transcriptionService;
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

            // Step 4: Extract audio using IAudioExtractionService
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
                Progress = 10,
                CurrentStage = "Extracting audio",
                StatusMessage = "Downloading and extracting audio from video",
                UpdatedAt = DateTime.UtcNow
            });

            audioFilePath = await _audioExtractionService.ExtractAudioFromYouTubeAsync(video.YouTubeId, cancellationToken);

            // Get audio info
            var audioInfo = await _audioExtractionService.GetAudioInfoAsync(audioFilePath, cancellationToken);
            var audioExtractionDuration = (DateTime.UtcNow - audioExtractionStartTime).TotalSeconds;
            _logger.LogInformation("Stage: Audio extraction completed for video: {VideoId}, JobId: {JobId} in {DurationSeconds}s. Duration: {AudioDuration}, Size: {Size}",
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
                StatusMessage = $"Audio extracted successfully. Duration: {audioInfo.Duration}",
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
                await UpdateJobStatusAsync(transcriptionJob, JobStatus.Failed, ex.Message, cancellationToken);

                // Notify: Job failed
                await _progressNotificationService.NotifyJobFailedAsync(
                    transcriptionJob.Id,
                    videoId,
                    ex.Message);
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

        // Create new segments
        var segmentCount = 0;
        for (int i = 0; i < transcriptionResult.Segments.Count; i++)
        {
            var segmentDto = transcriptionResult.Segments[i];
            var segment = new TranscriptSegment
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = video.Id,
                SegmentIndex = i,
                StartTime = segmentDto.StartTime,
                EndTime = segmentDto.EndTime,
                Text = segmentDto.Text.Trim(),
                Language = transcriptionResult.Language,
                Confidence = segmentDto.Confidence,
                Speaker = segmentDto.Speaker,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _transcriptSegmentRepository.AddAsync(segment);
            segmentCount++;
        }

        _logger.LogInformation("Saved {Count} transcript segments for video: {VideoId}",
            segmentCount, video.Id);
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
}