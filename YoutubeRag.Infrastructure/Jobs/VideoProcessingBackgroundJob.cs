using Microsoft.Extensions.Logging;
using YoutubeRag.Application.Services;
using YoutubeRag.Application.Configuration;
using YoutubeRag.Application.Interfaces.Services;
using YoutubeRag.Infrastructure.Services;
using Hangfire;

namespace YoutubeRag.Infrastructure.Jobs;

/// <summary>
/// Hangfire background job wrapper for complete video processing pipeline
/// </summary>
public class VideoProcessingBackgroundJob
{
    private readonly TranscriptionJobProcessor _transcriptionProcessor;
    private readonly EmbeddingJobProcessor _embeddingProcessor;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly IAppConfiguration _appConfiguration;
    private readonly ILogger<VideoProcessingBackgroundJob> _logger;

    public VideoProcessingBackgroundJob(
        TranscriptionJobProcessor transcriptionProcessor,
        EmbeddingJobProcessor embeddingProcessor,
        IBackgroundJobService backgroundJobService,
        IAppConfiguration appConfiguration,
        ILogger<VideoProcessingBackgroundJob> logger)
    {
        _transcriptionProcessor = transcriptionProcessor;
        _embeddingProcessor = embeddingProcessor;
        _backgroundJobService = backgroundJobService;
        _appConfiguration = appConfiguration;
        _logger = logger;
    }

    /// <summary>
    /// Execute the complete video processing pipeline (transcription + embeddings)
    /// </summary>
    /// <param name="videoId">The video ID to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 30, 60 })]
    [Queue("default")]
    public async Task ExecuteAsync(string videoId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting complete video processing pipeline for video: {VideoId}", videoId);

        try
        {
            // Step 1: Run transcription
            _logger.LogInformation("Processing transcription for video: {VideoId}", videoId);
            var transcriptionSuccess = await _transcriptionProcessor.ProcessTranscriptionJobAsync(videoId, cancellationToken);

            if (!transcriptionSuccess)
            {
                _logger.LogError("Transcription failed for video: {VideoId}. Stopping pipeline.", videoId);
                throw new InvalidOperationException($"Transcription failed for video: {videoId}");
            }

            // Step 2: Run embedding generation if configured
            if (_appConfiguration.AutoGenerateEmbeddings)
            {
                _logger.LogInformation("Processing embeddings for video: {VideoId}", videoId);
                var embeddingSuccess = await _embeddingProcessor.ProcessEmbeddingJobAsync(videoId, jobId: null, cancellationToken);

                if (!embeddingSuccess)
                {
                    _logger.LogWarning("Embedding generation failed for video: {VideoId}", videoId);
                    // Don't fail the entire pipeline if embeddings fail
                }
            }
            else
            {
                _logger.LogInformation("Skipping embedding generation (AutoGenerateEmbeddings is disabled)");
            }

            _logger.LogInformation("Completed video processing pipeline for video: {VideoId}", videoId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in video processing pipeline for video: {VideoId}", videoId);
            throw;
        }
    }
}