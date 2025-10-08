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
}
