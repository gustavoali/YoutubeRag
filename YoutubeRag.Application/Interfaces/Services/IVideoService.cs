using YoutubeRag.Application.DTOs.Common;
using YoutubeRag.Application.DTOs.Video;

namespace YoutubeRag.Application.Interfaces.Services;

/// <summary>
/// Service interface for video management operations
/// </summary>
public interface IVideoService
{
    /// <summary>
    /// Get video by ID
    /// </summary>
    Task<VideoDto?> GetByIdAsync(string id);

    /// <summary>
    /// Get paginated list of videos
    /// </summary>
    Task<PaginatedResultDto<VideoListDto>> GetAllAsync(int page = 1, int pageSize = 10, string? userId = null);

    /// <summary>
    /// Create a new video
    /// </summary>
    Task<VideoDto> CreateAsync(CreateVideoDto createDto, string userId);

    /// <summary>
    /// Update an existing video
    /// </summary>
    Task<VideoDto> UpdateAsync(string id, UpdateVideoDto updateDto);

    /// <summary>
    /// Delete a video
    /// </summary>
    Task DeleteAsync(string id);

    /// <summary>
    /// Get video details with related entities
    /// </summary>
    Task<VideoDetailsDto> GetDetailsAsync(string id);

    /// <summary>
    /// Get video statistics
    /// </summary>
    Task<VideoStatsDto> GetStatsAsync(string id);

    /// <summary>
    /// Get videos by user ID
    /// </summary>
    Task<List<VideoListDto>> GetByUserIdAsync(string userId);

    /// <summary>
    /// Submit a YouTube video URL for processing
    /// </summary>
    /// <param name="submitDto">The submission request containing the YouTube URL</param>
    /// <param name="userId">The ID of the user submitting the video</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The submission result containing video and job information</returns>
    Task<VideoSubmissionResultDto> SubmitVideoFromUrlAsync(SubmitVideoDto submitDto, string userId, CancellationToken cancellationToken = default);
}
