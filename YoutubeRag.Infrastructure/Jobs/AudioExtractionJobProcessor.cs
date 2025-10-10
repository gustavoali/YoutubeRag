using Microsoft.Extensions.Logging;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;
using Hangfire;

namespace YoutubeRag.Infrastructure.Jobs;

/// <summary>
/// Processor for the Audio Extraction stage of the transcription pipeline
/// Extracts Whisper-compatible audio from downloaded video
/// </summary>
public class AudioExtractionJobProcessor
{
    private readonly IAudioExtractionService _audioExtractionService;
    private readonly IVideoRepository _videoRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<AudioExtractionJobProcessor> _logger;

    public AudioExtractionJobProcessor(
        IAudioExtractionService audioExtractionService,
        IVideoRepository videoRepository,
        IJobRepository jobRepository,
        IUnitOfWork unitOfWork,
        IBackgroundJobClient backgroundJobClient,
        ILogger<AudioExtractionJobProcessor> logger)
    {
        _audioExtractionService = audioExtractionService;
        _videoRepository = videoRepository;
        _jobRepository = jobRepository;
        _unitOfWork = unitOfWork;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    /// <summary>
    /// Executes the audio extraction stage for a video job
    /// </summary>
    /// <param name="jobId">The job ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [AutomaticRetry(Attempts = 0)] // We handle retries ourselves with retry policies
    [Queue("default")]
    public async Task ExecuteAsync(string jobId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Audio Extraction stage for job: {JobId}", jobId);

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
            job.CurrentStage = PipelineStage.AudioExtraction;
            job.Status = JobStatus.Running;
            job.SetStageProgress(PipelineStage.AudioExtraction, 0);
            job.Progress = job.CalculateOverallProgress();
            await _jobRepository.UpdateAsync(job);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Get video file path from metadata
            var metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(job.Metadata ?? "{}")
                ?? new Dictionary<string, object>();

            if (!metadata.TryGetValue("VideoFilePath", out var videoFilePathObj))
            {
                throw new InvalidOperationException("Video file path not found in job metadata");
            }

            var videoFilePath = videoFilePathObj.ToString()!;
            _logger.LogInformation("Extracting audio from video {Path} for job {JobId}", videoFilePath, jobId);

            // Extract Whisper-compatible audio (16kHz mono WAV)
            var audioFilePath = await _audioExtractionService.ExtractWhisperAudioFromVideoAsync(
                videoFilePath,
                video.Id,
                cancellationToken);

            _logger.LogInformation("Audio extraction completed for job {JobId}. Audio path: {Path}", jobId, audioFilePath);

            // Get audio info
            var audioInfo = await _audioExtractionService.GetAudioInfoAsync(audioFilePath, cancellationToken);
            _logger.LogInformation("Audio info for job {JobId}: Duration={Duration}, Size={Size}",
                jobId, audioInfo.Duration, audioInfo.FormattedFileSize);

            // Mark audio extraction stage as complete
            job.SetStageProgress(PipelineStage.AudioExtraction, 100);
            job.Progress = job.CalculateOverallProgress();

            // Store audio file path in job metadata
            metadata["AudioFilePath"] = audioFilePath;
            metadata["AudioDuration"] = audioInfo.Duration.ToString();
            metadata["AudioSizeBytes"] = audioInfo.FileSizeBytes;
            job.Metadata = System.Text.Json.JsonSerializer.Serialize(metadata);

            await _jobRepository.UpdateAsync(job);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Enqueue next stage: Transcription
            _logger.LogInformation("Enqueueing Transcription stage for job {JobId}", jobId);
            _backgroundJobClient.Enqueue<TranscriptionStageJobProcessor>(
                processor => processor.ExecuteAsync(jobId, CancellationToken.None));

            _logger.LogInformation("Audio Extraction stage completed successfully for job {JobId}", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audio Extraction stage failed for job {JobId}: {Error}", jobId, ex.Message);

            job.Status = JobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.SetStageProgress(PipelineStage.AudioExtraction, 0);
            await _jobRepository.UpdateAsync(job);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw;
        }
    }
}
