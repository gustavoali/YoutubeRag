using YoutubeRag.Application.DTOs.Video;

namespace YoutubeRag.Application.Interfaces;

/// <summary>
/// Service for extracting metadata from YouTube videos
/// </summary>
public interface IMetadataExtractionService
{
    /// <summary>
    /// Extracts comprehensive metadata from a YouTube video
    /// </summary>
    /// <param name="youTubeId">The YouTube video ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Video metadata or null if extraction fails</returns>
    Task<VideoMetadataDto> ExtractMetadataAsync(string youTubeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a YouTube video is accessible (not private, deleted, or region-blocked)
    /// </summary>
    /// <param name="youTubeId">The YouTube video ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the video is accessible, false otherwise</returns>
    Task<bool> IsVideoAccessibleAsync(string youTubeId, CancellationToken cancellationToken = default);
}
