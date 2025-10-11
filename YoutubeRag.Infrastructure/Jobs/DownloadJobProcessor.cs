using Hangfire;
using Microsoft.Extensions.Logging;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Infrastructure.Jobs;

/// <summary>
/// Processor for the Download stage of the transcription pipeline
/// Downloads video from YouTube
/// </summary>
public class DownloadJobProcessor
{
    private readonly IVideoDownloadService _videoDownloadService;
    private readonly IVideoRepository _videoRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<DownloadJobProcessor> _logger;

    public DownloadJobProcessor(
        IVideoDownloadService videoDownloadService,
        IVideoRepository videoRepository,
        IJobRepository jobRepository,
        IUnitOfWork unitOfWork,
        IBackgroundJobClient backgroundJobClient,
        ILogger<DownloadJobProcessor> logger)
    {
        _videoDownloadService = videoDownloadService;
        _videoRepository = videoRepository;
        _jobRepository = jobRepository;
        _unitOfWork = unitOfWork;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    /// <summary>
    /// Executes the download stage for a video job
    /// </summary>
    /// <param name="jobId">The job ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [AutomaticRetry(Attempts = 0)] // We handle retries ourselves with retry policies
    [Queue("default")]
    public async Task ExecuteAsync(string jobId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Download stage for job: {JobId}", jobId);

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
            job.CurrentStage = PipelineStage.Download;
            job.Status = JobStatus.Running;
            job.SetStageProgress(PipelineStage.Download, 0);
            job.Progress = job.CalculateOverallProgress();
            await _jobRepository.UpdateAsync(job);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Downloading video {YouTubeId} for job {JobId}", video.YouTubeId, jobId);

            // Download video with progress tracking
            var videoFilePath = await _videoDownloadService.DownloadVideoAsync(
                video.YouTubeId,
                progress: new Progress<double>(p =>
                {
                    job.SetStageProgress(PipelineStage.Download, p * 100);
                    job.Progress = job.CalculateOverallProgress();
                    _jobRepository.UpdateAsync(job).GetAwaiter().GetResult();
                    _unitOfWork.SaveChangesAsync(cancellationToken).GetAwaiter().GetResult();
                }),
                cancellationToken);

            _logger.LogInformation("Download completed for job {JobId}. Video path: {Path}", jobId, videoFilePath);

            // Mark download stage as complete
            job.SetStageProgress(PipelineStage.Download, 100);
            job.Progress = job.CalculateOverallProgress();

            // Store video file path in job metadata
            var metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(job.Metadata ?? "{}")
                ?? new Dictionary<string, object>();
            metadata["VideoFilePath"] = videoFilePath;
            job.Metadata = System.Text.Json.JsonSerializer.Serialize(metadata);

            await _jobRepository.UpdateAsync(job);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Enqueue next stage: AudioExtraction
            _logger.LogInformation("Enqueueing AudioExtraction stage for job {JobId}", jobId);
            _backgroundJobClient.Enqueue<AudioExtractionJobProcessor>(
                processor => processor.ExecuteAsync(jobId, CancellationToken.None));

            _logger.LogInformation("Download stage completed successfully for job {JobId}", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Download stage failed for job {JobId}: {Error}", jobId, ex.Message);

            job.Status = JobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.SetStageProgress(PipelineStage.Download, 0);
            await _jobRepository.UpdateAsync(job);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw;
        }
    }
}
