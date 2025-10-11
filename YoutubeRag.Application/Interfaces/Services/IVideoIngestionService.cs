using YoutubeRag.Application.DTOs.Video;

namespace YoutubeRag.Application.Interfaces.Services;

public interface IVideoIngestionService
{
    /// <summary>
    /// Initiates video ingestion from a YouTube URL
    /// </summary>
    Task<VideoIngestionResponse> IngestVideoFromUrlAsync(VideoIngestionRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a YouTube URL is valid and accessible
    /// </summary>
    Task<(bool IsValid, string? YouTubeId, string? ErrorMessage)> ValidateYouTubeUrlAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts YouTube video ID from various URL formats
    /// </summary>
    string? ExtractYouTubeId(string url);

    /// <summary>
    /// Checks if a video has already been ingested
    /// </summary>
    Task<bool> IsVideoAlreadyIngestedAsync(string youTubeId, CancellationToken cancellationToken = default);
}
