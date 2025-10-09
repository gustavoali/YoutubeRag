using Microsoft.Extensions.Logging;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Application.DTOs.Transcription;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;
using Hangfire;

namespace YoutubeRag.Infrastructure.Jobs;

/// <summary>
/// Processor for the Transcription stage of the transcription pipeline
/// Transcribes audio using Whisper
/// </summary>
public class TranscriptionStageJobProcessor
{
    private readonly ITranscriptionService _transcriptionService;
    private readonly IVideoRepository _videoRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<TranscriptionStageJobProcessor> _logger;

    public TranscriptionStageJobProcessor(
        ITranscriptionService transcriptionService,
        IVideoRepository videoRepository,
        IJobRepository jobRepository,
        IUnitOfWork unitOfWork,
        IBackgroundJobClient backgroundJobClient,
        ILogger<TranscriptionStageJobProcessor> logger)
    {
        _transcriptionService = transcriptionService;
        _videoRepository = videoRepository;
        _jobRepository = jobRepository;
        _unitOfWork = unitOfWork;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    /// <summary>
    /// Executes the transcription stage for a video job
    /// </summary>
    /// <param name="jobId">The job ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [AutomaticRetry(Attempts = 0)] // We handle retries ourselves with retry policies
    [Queue("default")]
    public async Task ExecuteAsync(string jobId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Transcription stage for job: {JobId}", jobId);

        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job == null)
        {
            _logger.LogError("Job {JobId} not found", jobId);
            throw new InvalidOperationException($"Job {jobId} not found");
        }

        var video = await _videoRepository.GetByIdAsync(job.VideoId!);
        if (video == null)
        {
            _logger.LogError("Video {VideoId} not found for job {JobId}", job.VideoId, jobId);
            throw new InvalidOperationException($"Video {job.VideoId} not found");
        }

        try
        {
            // Update job stage
            job.CurrentStage = PipelineStage.Transcription;
            job.Status = JobStatus.Running;
            job.SetStageProgress(PipelineStage.Transcription, 0);
            job.Progress = job.CalculateOverallProgress();
            await _jobRepository.UpdateAsync(job);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Check if Whisper is available
            if (!await _transcriptionService.IsWhisperAvailableAsync())
            {
                throw new InvalidOperationException("Whisper transcription service is not available");
            }

            // Get audio file path from metadata
            var metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(job.Metadata ?? "{}")
                ?? new Dictionary<string, object>();

            if (!metadata.TryGetValue("AudioFilePath", out var audioFilePathObj))
            {
                throw new InvalidOperationException("Audio file path not found in job metadata");
            }

            var audioFilePath = audioFilePathObj.ToString()!;
            _logger.LogInformation("Transcribing audio {Path} for job {JobId}", audioFilePath, jobId);

            // Parse audio duration
            TimeSpan audioDuration = TimeSpan.Zero;
            if (metadata.TryGetValue("AudioDuration", out var durationObj))
            {
                TimeSpan.TryParse(durationObj.ToString(), out audioDuration);
            }

            // Determine transcription quality based on duration
            var quality = DetermineTranscriptionQuality(audioDuration);

            // Create transcription request
            var transcriptionRequest = new TranscriptionRequestDto(
                VideoId: video.Id,
                AudioFilePath: audioFilePath,
                Language: video.Language ?? "auto",
                Quality: quality
            );

            // Transcribe audio
            var transcriptionResult = await _transcriptionService.TranscribeAudioAsync(
                transcriptionRequest,
                cancellationToken);

            _logger.LogInformation("Transcription completed for job {JobId}. Segments: {Count}, Language: {Language}",
                jobId, transcriptionResult.Segments.Count, transcriptionResult.Language);

            // Mark transcription stage as complete
            job.SetStageProgress(PipelineStage.Transcription, 100);
            job.Progress = job.CalculateOverallProgress();

            // Store transcription result in job metadata
            metadata["TranscriptionSegmentCount"] = transcriptionResult.Segments.Count;
            metadata["TranscriptionLanguage"] = transcriptionResult.Language;
            metadata["TranscriptionDuration"] = transcriptionResult.Duration.ToString();
            metadata["TranscriptionResultJson"] = System.Text.Json.JsonSerializer.Serialize(transcriptionResult);
            job.Metadata = System.Text.Json.JsonSerializer.Serialize(metadata);

            // Update video
            video.Duration = transcriptionResult.Duration;
            video.Language = transcriptionResult.Language;
            await _videoRepository.UpdateAsync(video);

            await _jobRepository.UpdateAsync(job);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Enqueue next stage: Segmentation
            _logger.LogInformation("Enqueueing Segmentation stage for job {JobId}", jobId);
            _backgroundJobClient.Enqueue<SegmentationJobProcessor>(
                processor => processor.ExecuteAsync(jobId, CancellationToken.None));

            _logger.LogInformation("Transcription stage completed successfully for job {JobId}", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transcription stage failed for job {JobId}: {Error}", jobId, ex.Message);

            job.Status = JobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.SetStageProgress(PipelineStage.Transcription, 0);
            await _jobRepository.UpdateAsync(job);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw;
        }
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
