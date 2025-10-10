using Microsoft.Extensions.Logging;
using YoutubeRag.Application.Services;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Domain.Enums;
using Hangfire;

namespace YoutubeRag.Infrastructure.Jobs;

/// <summary>
/// Hangfire background job wrapper for transcription processing
/// </summary>
public class TranscriptionBackgroundJob
{
    private readonly TranscriptionJobProcessor _processor;
    private readonly IJobRepository _jobRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TranscriptionBackgroundJob> _logger;

    public TranscriptionBackgroundJob(
        TranscriptionJobProcessor processor,
        IJobRepository jobRepository,
        IUnitOfWork unitOfWork,
        ILogger<TranscriptionBackgroundJob> logger)
    {
        _processor = processor;
        _jobRepository = jobRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Execute the transcription job for a video
    /// </summary>
    /// <param name="videoId">The video ID to transcribe</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 10, 30, 60 })]
    [Queue("default")]
    public async Task ExecuteAsync(string videoId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Hangfire transcription job for video: {VideoId}", videoId);

        try
        {
            // Execute the actual transcription processing
            var success = await _processor.ProcessTranscriptionJobAsync(videoId, cancellationToken);

            if (!success)
            {
                throw new InvalidOperationException($"Transcription job failed for video: {videoId}");
            }

            _logger.LogInformation("Completed Hangfire transcription job for video: {VideoId}", videoId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Hangfire transcription job for video: {VideoId}", videoId);
            throw;
        }
    }

    private async Task UpdateJobHangfireId(string videoId, string hangfireJobId, JobType jobType, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(hangfireJobId))
            return;

        try
        {
            var jobs = await _jobRepository.GetByVideoIdAsync(videoId);
            var job = jobs.FirstOrDefault(j => j.Type == jobType && j.Status != JobStatus.Completed);

            if (job != null)
            {
                job.HangfireJobId = hangfireJobId;
                job.UpdatedAt = DateTime.UtcNow;
                await _jobRepository.UpdateAsync(job);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogDebug("Updated job {JobId} with Hangfire ID {HangfireJobId}", job.Id, hangfireJobId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update Hangfire job ID for video {VideoId}", videoId);
            // Don't fail the job for this
        }
    }
}