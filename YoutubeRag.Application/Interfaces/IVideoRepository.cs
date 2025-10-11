using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Application.Interfaces;

/// <summary>
/// Repository interface for Video entity operations
/// </summary>
public interface IVideoRepository : IRepository<Video>
{
    /// <summary>
    /// Gets a video by its YouTube identifier
    /// </summary>
    /// <param name="youtubeId">The YouTube video identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The video if found; otherwise, null</returns>
    Task<Video?> GetByYouTubeIdAsync(string youtubeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all videos for a specific user
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <returns>A collection of videos belonging to the user</returns>
    Task<IEnumerable<Video>> GetByUserIdAsync(string userId);

    /// <summary>
    /// Gets a video with its associated transcript segments
    /// </summary>
    /// <param name="videoId">The video's unique identifier</param>
    /// <returns>The video with loaded transcript segments if found; otherwise, null</returns>
    Task<Video?> GetWithTranscriptsAsync(string videoId);

    /// <summary>
    /// Gets a video with all its related data (transcripts, jobs, user)
    /// </summary>
    /// <param name="videoId">The video's unique identifier</param>
    /// <returns>The video with all related data if found; otherwise, null</returns>
    Task<Video?> GetWithAllRelatedDataAsync(string videoId);

    /// <summary>
    /// Gets videos by their processing status
    /// </summary>
    /// <param name="status">The video processing status</param>
    /// <returns>A collection of videos with the specified status</returns>
    Task<IEnumerable<Video>> GetByStatusAsync(VideoStatus status);

    /// <summary>
    /// Gets videos for a user filtered by status
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <param name="status">The video processing status</param>
    /// <returns>A collection of videos matching the criteria</returns>
    Task<IEnumerable<Video>> GetByUserIdAndStatusAsync(string userId, VideoStatus status);

    /// <summary>
    /// Searches videos by title
    /// </summary>
    /// <param name="searchTerm">The search term to look for in titles</param>
    /// <returns>A collection of videos with matching titles</returns>
    Task<IEnumerable<Video>> SearchByTitleAsync(string searchTerm);

    /// <summary>
    /// Gets paginated videos for a user
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <param name="pageNumber">The page number (1-based)</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <returns>A collection of paginated videos</returns>
    Task<IEnumerable<Video>> GetPaginatedByUserIdAsync(string userId, int pageNumber, int pageSize);

    /// <summary>
    /// Gets videos that were processed within a specific date range
    /// </summary>
    /// <param name="startDate">The start date of the range</param>
    /// <param name="endDate">The end date of the range</param>
    /// <returns>A collection of videos processed within the date range</returns>
    Task<IEnumerable<Video>> GetByProcessedDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Checks if a YouTube video already exists for a user
    /// </summary>
    /// <param name="youtubeId">The YouTube video identifier</param>
    /// <param name="userId">The user's unique identifier</param>
    /// <returns>True if the video exists for the user; otherwise, false</returns>
    Task<bool> ExistsForUserAsync(string youtubeId, string userId);
}
