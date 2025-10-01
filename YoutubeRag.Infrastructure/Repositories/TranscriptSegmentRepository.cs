using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Infrastructure.Data;

namespace YoutubeRag.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for TranscriptSegment entity operations
/// </summary>
public class TranscriptSegmentRepository : Repository<TranscriptSegment>, ITranscriptSegmentRepository
{
    /// <summary>
    /// Initializes a new instance of the TranscriptSegmentRepository class
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="logger">The logger instance</param>
    public TranscriptSegmentRepository(ApplicationDbContext context, ILogger<TranscriptSegmentRepository> logger)
        : base(context, logger)
    {
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TranscriptSegment>> GetByVideoIdAsync(string videoId)
    {
        if (string.IsNullOrWhiteSpace(videoId))
        {
            throw new ArgumentException("Video ID cannot be null or empty", nameof(videoId));
        }

        try
        {
            return await _dbSet
                .Where(ts => ts.VideoId == videoId)
                .OrderBy(ts => ts.SegmentIndex)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transcript segments for video ID {VideoId}", videoId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TranscriptSegment>> SearchByTextAsync(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            throw new ArgumentException("Search text cannot be null or empty", nameof(searchText));
        }

        try
        {
            var lowerSearchText = searchText.ToLower();
            return await _dbSet
                .Where(ts => ts.Text.ToLower().Contains(lowerSearchText))
                .Include(ts => ts.Video)
                .OrderBy(ts => ts.VideoId)
                .ThenBy(ts => ts.SegmentIndex)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching transcript segments with text {SearchText}", searchText);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TranscriptSegment>> SearchByTextInVideoAsync(string videoId, string searchText)
    {
        if (string.IsNullOrWhiteSpace(videoId))
        {
            throw new ArgumentException("Video ID cannot be null or empty", nameof(videoId));
        }

        if (string.IsNullOrWhiteSpace(searchText))
        {
            throw new ArgumentException("Search text cannot be null or empty", nameof(searchText));
        }

        try
        {
            var lowerSearchText = searchText.ToLower();
            return await _dbSet
                .Where(ts => ts.VideoId == videoId && ts.Text.ToLower().Contains(lowerSearchText))
                .OrderBy(ts => ts.SegmentIndex)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching transcript segments in video {VideoId} with text {SearchText}",
                videoId, searchText);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TranscriptSegment>> GetByVideoIdAndTimeRangeAsync(string videoId, double startTime, double endTime)
    {
        if (string.IsNullOrWhiteSpace(videoId))
        {
            throw new ArgumentException("Video ID cannot be null or empty", nameof(videoId));
        }

        if (startTime < 0)
        {
            throw new ArgumentException("Start time cannot be negative", nameof(startTime));
        }

        if (endTime < startTime)
        {
            throw new ArgumentException("End time must be after start time", nameof(endTime));
        }

        try
        {
            return await _dbSet
                .Where(ts => ts.VideoId == videoId &&
                            ts.StartTime >= startTime &&
                            ts.EndTime <= endTime)
                .OrderBy(ts => ts.SegmentIndex)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transcript segments for video {VideoId} between {StartTime} and {EndTime}",
                videoId, startTime, endTime);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TranscriptSegment>> GetByVideoIdAndLanguageAsync(string videoId, string language)
    {
        if (string.IsNullOrWhiteSpace(videoId))
        {
            throw new ArgumentException("Video ID cannot be null or empty", nameof(videoId));
        }

        if (string.IsNullOrWhiteSpace(language))
        {
            throw new ArgumentException("Language cannot be null or empty", nameof(language));
        }

        try
        {
            return await _dbSet
                .Where(ts => ts.VideoId == videoId && ts.Language == language)
                .OrderBy(ts => ts.SegmentIndex)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transcript segments for video {VideoId} in language {Language}",
                videoId, language);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TranscriptSegment>> GetPaginatedByVideoIdAsync(string videoId, int pageNumber, int pageSize)
    {
        if (string.IsNullOrWhiteSpace(videoId))
        {
            throw new ArgumentException("Video ID cannot be null or empty", nameof(videoId));
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
                .Where(ts => ts.VideoId == videoId)
                .OrderBy(ts => ts.SegmentIndex)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated transcript segments for video {VideoId}, page {PageNumber}, size {PageSize}",
                videoId, pageNumber, pageSize);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<TranscriptSegment?> GetByVideoIdAndIndexAsync(string videoId, int segmentIndex)
    {
        if (string.IsNullOrWhiteSpace(videoId))
        {
            throw new ArgumentException("Video ID cannot be null or empty", nameof(videoId));
        }

        if (segmentIndex < 0)
        {
            throw new ArgumentException("Segment index cannot be negative", nameof(segmentIndex));
        }

        try
        {
            return await _dbSet
                .FirstOrDefaultAsync(ts => ts.VideoId == videoId && ts.SegmentIndex == segmentIndex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transcript segment for video {VideoId} at index {SegmentIndex}",
                videoId, segmentIndex);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> DeleteByVideoIdAsync(string videoId)
    {
        if (string.IsNullOrWhiteSpace(videoId))
        {
            throw new ArgumentException("Video ID cannot be null or empty", nameof(videoId));
        }

        try
        {
            var segments = await _dbSet
                .Where(ts => ts.VideoId == videoId)
                .ToListAsync();

            if (segments.Any())
            {
                _dbSet.RemoveRange(segments);
            }

            return segments.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting transcript segments for video {VideoId}", videoId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<double> GetVideoDurationAsync(string videoId)
    {
        if (string.IsNullOrWhiteSpace(videoId))
        {
            throw new ArgumentException("Video ID cannot be null or empty", nameof(videoId));
        }

        try
        {
            var lastSegment = await _dbSet
                .Where(ts => ts.VideoId == videoId)
                .OrderByDescending(ts => ts.EndTime)
                .FirstOrDefaultAsync();

            return lastSegment?.EndTime ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting video duration for video {VideoId}", videoId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TranscriptSegment>> SearchBySimilarityAsync(float[] embedding, int limit = 10, float threshold = 0.7f)
    {
        if (embedding == null || embedding.Length == 0)
        {
            throw new ArgumentException("Embedding cannot be null or empty", nameof(embedding));
        }

        if (limit < 1)
        {
            throw new ArgumentException("Limit must be greater than 0", nameof(limit));
        }

        if (threshold < 0 || threshold > 1)
        {
            throw new ArgumentException("Threshold must be between 0 and 1", nameof(threshold));
        }

        try
        {
            // Note: This is a placeholder implementation
            // In a real scenario, you would use a vector database or implement cosine similarity
            // For now, returning segments that have embeddings
            return await _dbSet
                .Where(ts => ts.EmbeddingVector != null)
                .Take(limit)
                .Include(ts => ts.Video)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching transcript segments by similarity");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TranscriptSegment>> GetWithEmbeddingsByVideoIdAsync(string videoId)
    {
        if (string.IsNullOrWhiteSpace(videoId))
        {
            throw new ArgumentException("Video ID cannot be null or empty", nameof(videoId));
        }

        try
        {
            return await _dbSet
                .Where(ts => ts.VideoId == videoId && ts.EmbeddingVector != null)
                .OrderBy(ts => ts.SegmentIndex)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transcript segments with embeddings for video {VideoId}", videoId);
            throw;
        }
    }
}