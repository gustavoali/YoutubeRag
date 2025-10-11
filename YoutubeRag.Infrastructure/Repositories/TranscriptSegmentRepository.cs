using System.Text.Json;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Infrastructure.Data;
using YoutubeRag.Infrastructure.Services;

namespace YoutubeRag.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing transcript segments
/// </summary>
public class TranscriptSegmentRepository : ITranscriptSegmentRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TranscriptSegmentRepository> _logger;

    public TranscriptSegmentRepository(
        ApplicationDbContext context,
        ILogger<TranscriptSegmentRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TranscriptSegment?> GetByIdAsync(string segmentId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(segmentId, nameof(segmentId));

        return await _context.TranscriptSegments
            .Include(s => s.Video)
            .FirstOrDefaultAsync(s => s.Id == segmentId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<TranscriptSegment>> GetByVideoIdAsync(string videoId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId, nameof(videoId));

        return await _context.TranscriptSegments
            .Where(s => s.VideoId == videoId)
            .OrderBy(s => s.SegmentIndex)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<TranscriptSegment>> GetSegmentsWithoutEmbeddingsAsync(
        string videoId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId, nameof(videoId));

        return await _context.TranscriptSegments
            .Where(s => s.VideoId == videoId &&
                       (s.EmbeddingVector == null || s.EmbeddingVector == ""))
            .OrderBy(s => s.SegmentIndex)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<TranscriptSegment>> GetSegmentsWithEmbeddingsAsync(
        string videoId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId, nameof(videoId));

        return await _context.TranscriptSegments
            .Where(s => s.VideoId == videoId &&
                       s.EmbeddingVector != null &&
                       s.EmbeddingVector != "")
            .OrderBy(s => s.SegmentIndex)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> UpdateEmbeddingsAsync(
        List<(string segmentId, float[] embedding)> embeddings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(embeddings, nameof(embeddings));

        if (!embeddings.Any())
        {
            return 0;
        }

        _logger.LogDebug("Updating embeddings for {Count} segments", embeddings.Count);

        var segmentIds = embeddings.Select(e => e.segmentId).ToList();
        var segments = await _context.TranscriptSegments
            .Where(s => segmentIds.Contains(s.Id))
            .ToListAsync(cancellationToken);

        var embeddingDict = embeddings.ToDictionary(e => e.segmentId, e => e.embedding);
        int updatedCount = 0;

        foreach (var segment in segments)
        {
            if (embeddingDict.TryGetValue(segment.Id, out var embedding))
            {
                segment.EmbeddingVector = LocalEmbeddingService.SerializeEmbedding(embedding);
                segment.UpdatedAt = DateTime.UtcNow;
                updatedCount++;
            }
        }

        if (updatedCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Successfully updated embeddings for {Count} segments", updatedCount);
        }

        return updatedCount;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateEmbeddingAsync(
        string segmentId,
        float[] embedding,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(segmentId, nameof(segmentId));
        ArgumentNullException.ThrowIfNull(embedding, nameof(embedding));

        var segment = await _context.TranscriptSegments
            .FirstOrDefaultAsync(s => s.Id == segmentId, cancellationToken);

        if (segment == null)
        {
            _logger.LogWarning("Segment {SegmentId} not found for embedding update", segmentId);
            return false;
        }

        segment.EmbeddingVector = LocalEmbeddingService.SerializeEmbedding(embedding);
        segment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("Updated embedding for segment {SegmentId}", segmentId);

        return true;
    }

    /// <inheritdoc />
    public async Task<TranscriptSegment> AddAsync(TranscriptSegment segment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(segment, nameof(segment));

        if (string.IsNullOrWhiteSpace(segment.Id))
        {
            segment.Id = Guid.NewGuid().ToString();
        }

        segment.CreatedAt = DateTime.UtcNow;
        segment.UpdatedAt = DateTime.UtcNow;

        _context.TranscriptSegments.Add(segment);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Added transcript segment {SegmentId} for video {VideoId}",
            segment.Id, segment.VideoId);

        return segment;
    }

    /// <inheritdoc />
    public async Task<List<TranscriptSegment>> AddRangeAsync(
        List<TranscriptSegment> segments,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(segments, nameof(segments));

        if (!segments.Any())
        {
            return segments;
        }

        // Check if using relational database provider (not in-memory)
        var isRelationalDatabase = _context.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory";

        // Use bulk insert for large batches (>100 segments) ONLY if using a relational database
        // EFCore.BulkExtensions doesn't support in-memory databases
        if (segments.Count > 100 && isRelationalDatabase)
        {
            _logger.LogDebug("Using bulk insert for {Count} segments (relational database)", segments.Count);
            return await BulkInsertAsync(segments, cancellationToken);
        }

        // Set shared timestamp for ALL segments to ensure true bulk insert behavior
        // Only set timestamps if they haven't been set already (to preserve caller-set values)
        var now = DateTime.UtcNow;
        foreach (var segment in segments)
        {
            if (string.IsNullOrWhiteSpace(segment.Id))
            {
                segment.Id = Guid.NewGuid().ToString();
            }

            // CRITICAL FIX (ISSUE-002): Only set timestamps if not already set
            // This ensures all segments have the SAME timestamp when bulk inserting
            if (segment.CreatedAt == default)
            {
                segment.CreatedAt = now;
            }

            if (segment.UpdatedAt == default)
            {
                segment.UpdatedAt = now;
            }
        }

        _context.TranscriptSegments.AddRange(segments);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added {Count} transcript segments (standard AddRange)", segments.Count);

        return segments;
    }

    /// <summary>
    /// Bulk inserts transcript segments using EFCore.BulkExtensions for optimal performance
    /// </summary>
    /// <param name="segments">List of segments to insert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Inserted segments</returns>
    public async Task<List<TranscriptSegment>> BulkInsertAsync(
        List<TranscriptSegment> segments,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(segments, nameof(segments));

        if (!segments.Any())
        {
            return segments;
        }

        // Set shared timestamp for ALL segments to ensure true bulk insert behavior
        // Only set timestamps if they haven't been set already (to preserve caller-set values)
        var now = DateTime.UtcNow;
        foreach (var segment in segments)
        {
            if (string.IsNullOrWhiteSpace(segment.Id))
            {
                segment.Id = Guid.NewGuid().ToString();
            }

            // CRITICAL FIX (ISSUE-002): Only set timestamps if not already set
            // This ensures all segments have the SAME timestamp when bulk inserting
            if (segment.CreatedAt == default)
            {
                segment.CreatedAt = now;
            }

            if (segment.UpdatedAt == default)
            {
                segment.UpdatedAt = now;
            }
        }

        var startTime = DateTime.UtcNow;

        // Use EFCore.BulkExtensions for high-performance bulk insert
        var bulkConfig = new BulkConfig
        {
            BatchSize = 1000,
            EnableStreaming = true,
            SetOutputIdentity = false, // We're using custom GUIDs, not auto-generated IDs
            TrackingEntities = false
        };

        await _context.BulkInsertAsync(segments, bulkConfig);

        var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
        _logger.LogInformation("Bulk inserted {Count} transcript segments in {Duration}ms ({Rate} segments/sec)",
            segments.Count, duration, (segments.Count / (duration / 1000.0)).ToString("F0"));

        return segments;
    }

    /// <inheritdoc />
    public async Task<TranscriptSegment> UpdateAsync(TranscriptSegment segment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(segment, nameof(segment));

        segment.UpdatedAt = DateTime.UtcNow;
        _context.TranscriptSegments.Update(segment);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Updated transcript segment {SegmentId}", segment.Id);

        return segment;
    }

    /// <inheritdoc />
    public async Task<int> DeleteByVideoIdAsync(string videoId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId, nameof(videoId));

        var segments = await _context.TranscriptSegments
            .Where(s => s.VideoId == videoId)
            .ToListAsync(cancellationToken);

        if (!segments.Any())
        {
            return 0;
        }

        _context.TranscriptSegments.RemoveRange(segments);
        var deletedCount = await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted {Count} transcript segments for video {VideoId}",
            segments.Count, videoId);

        return segments.Count;
    }

    /// <inheritdoc />
    public async Task<List<TranscriptSegment>> GetByTimeRangeAsync(
        string videoId,
        double startTime,
        double endTime,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId, nameof(videoId));

        return await _context.TranscriptSegments
            .Where(s => s.VideoId == videoId &&
                       s.StartTime >= startTime &&
                       s.EndTime <= endTime)
            .OrderBy(s => s.StartTime)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<TranscriptSegment>> SearchByTextAsync(
        string? videoId,
        string searchText,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchText, nameof(searchText));

        var query = _context.TranscriptSegments.AsQueryable();

        if (!string.IsNullOrWhiteSpace(videoId))
        {
            query = query.Where(s => s.VideoId == videoId);
        }

        // Use EF.Functions.Like for case-insensitive search
        query = query.Where(s => EF.Functions.Like(s.Text, $"%{searchText}%"));

        return await query
            .OrderBy(s => s.VideoId)
            .ThenBy(s => s.SegmentIndex)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetCountByVideoIdAsync(string videoId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId, nameof(videoId));

        return await _context.TranscriptSegments
            .CountAsync(s => s.VideoId == videoId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetEmbeddingCountByVideoIdAsync(string videoId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId, nameof(videoId));

        return await _context.TranscriptSegments
            .CountAsync(s => s.VideoId == videoId &&
                           s.EmbeddingVector != null &&
                           s.EmbeddingVector != "", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<TranscriptSegment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TranscriptSegments
            .OrderBy(s => s.VideoId)
            .ThenBy(s => s.SegmentIndex)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<TranscriptSegment>> FindAsync(
        System.Linq.Expressions.Expression<Func<TranscriptSegment, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate, nameof(predicate));

        return await _context.TranscriptSegments
            .Where(predicate)
            .OrderBy(s => s.VideoId)
            .ThenBy(s => s.SegmentIndex)
            .ToListAsync(cancellationToken);
    }
}
