using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;
using YoutubeRag.Infrastructure.Data;

namespace YoutubeRag.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Video entity operations
/// </summary>
public class VideoRepository : Repository<Video>, IVideoRepository
{
    /// <summary>
    /// Initializes a new instance of the VideoRepository class
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="logger">The logger instance</param>
    public VideoRepository(ApplicationDbContext context, ILogger<VideoRepository> logger)
        : base(context, logger)
    {
    }

    /// <inheritdoc />
    public async Task<Video?> GetByYouTubeIdAsync(string youtubeId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(youtubeId))
        {
            throw new ArgumentException("YouTube ID cannot be null or empty", nameof(youtubeId));
        }

        try
        {
            return await _dbSet
                .FirstOrDefaultAsync(v => v.YouTubeId == youtubeId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving video by YouTube ID {YoutubeId}", youtubeId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Video>> GetByUserIdAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        try
        {
            return await _dbSet
                .Where(v => v.UserId == userId)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving videos for user ID {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Video?> GetWithTranscriptsAsync(string videoId)
    {
        if (string.IsNullOrWhiteSpace(videoId))
        {
            throw new ArgumentException("Video ID cannot be null or empty", nameof(videoId));
        }

        try
        {
            return await _dbSet
                .Include(v => v.TranscriptSegments.OrderBy(ts => ts.SegmentIndex))
                .FirstOrDefaultAsync(v => v.Id == videoId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving video with transcripts for video ID {VideoId}", videoId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Video?> GetWithAllRelatedDataAsync(string videoId)
    {
        if (string.IsNullOrWhiteSpace(videoId))
        {
            throw new ArgumentException("Video ID cannot be null or empty", nameof(videoId));
        }

        try
        {
            return await _dbSet
                .Include(v => v.User)
                .Include(v => v.Jobs)
                .Include(v => v.TranscriptSegments.OrderBy(ts => ts.SegmentIndex))
                .FirstOrDefaultAsync(v => v.Id == videoId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving video with all related data for video ID {VideoId}", videoId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Video>> GetByStatusAsync(VideoStatus status)
    {
        try
        {
            return await _dbSet
                .Where(v => v.Status == status)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving videos by status {Status}", status);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Video>> GetByUserIdAndStatusAsync(string userId, VideoStatus status)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        try
        {
            return await _dbSet
                .Where(v => v.UserId == userId && v.Status == status)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving videos for user ID {UserId} with status {Status}", userId, status);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Video>> SearchByTitleAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            throw new ArgumentException("Search term cannot be null or empty", nameof(searchTerm));
        }

        try
        {
            var lowerSearchTerm = searchTerm.ToLower();
            return await _dbSet
                .Where(v => v.Title.ToLower().Contains(lowerSearchTerm))
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching videos by title with term {SearchTerm}", searchTerm);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Video>> GetPaginatedByUserIdAsync(string userId, int pageNumber, int pageSize)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        if (pageNumber < 1)
        {
            throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));
        }

        try
        {
            return await _dbSet
                .Where(v => v.UserId == userId)
                .OrderByDescending(v => v.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated videos for user ID {UserId}, page {PageNumber}, size {PageSize}",
                userId, pageNumber, pageSize);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Video>> GetByProcessedDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        if (endDate < startDate)
        {
            throw new ArgumentException("End date must be after start date", nameof(endDate));
        }

        try
        {
            return await _dbSet
                .Where(v => v.Status == VideoStatus.Completed &&
                           v.UpdatedAt >= startDate &&
                           v.UpdatedAt <= endDate)
                .OrderBy(v => v.UpdatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving videos processed between {StartDate} and {EndDate}", startDate, endDate);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsForUserAsync(string youtubeId, string userId)
    {
        if (string.IsNullOrWhiteSpace(youtubeId))
        {
            throw new ArgumentException("YouTube ID cannot be null or empty", nameof(youtubeId));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        try
        {
            return await _dbSet
                .AnyAsync(v => v.YouTubeId == youtubeId && v.UserId == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if video exists for YouTube ID {YoutubeId} and user ID {UserId}",
                youtubeId, userId);
            throw;
        }
    }
}