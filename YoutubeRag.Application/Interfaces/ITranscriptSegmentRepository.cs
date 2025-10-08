using YoutubeRag.Domain.Entities;

namespace YoutubeRag.Application.Interfaces;

/// <summary>
/// Repository interface for managing transcript segments
/// </summary>
public interface ITranscriptSegmentRepository
{
    /// <summary>
    /// Gets a transcript segment by ID
    /// </summary>
    /// <param name="segmentId">The segment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The transcript segment or null if not found</returns>
    Task<TranscriptSegment?> GetByIdAsync(string segmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all transcript segments for a video
    /// </summary>
    /// <param name="videoId">The video ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of transcript segments</returns>
    Task<List<TranscriptSegment>> GetByVideoIdAsync(string videoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets segments without embeddings for a specific video
    /// </summary>
    /// <param name="videoId">The video ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of segments without embeddings</returns>
    Task<List<TranscriptSegment>> GetSegmentsWithoutEmbeddingsAsync(
        string videoId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets segments with embeddings for a specific video
    /// </summary>
    /// <param name="videoId">The video ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of segments with embeddings</returns>
    Task<List<TranscriptSegment>> GetSegmentsWithEmbeddingsAsync(
        string videoId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates embeddings for multiple segments
    /// </summary>
    /// <param name="embeddings">List of segment IDs and their embeddings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of segments updated</returns>
    Task<int> UpdateEmbeddingsAsync(
        List<(string segmentId, float[] embedding)> embeddings,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a single segment's embedding
    /// </summary>
    /// <param name="segmentId">The segment ID</param>
    /// <param name="embedding">The embedding vector</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateEmbeddingAsync(
        string segmentId,
        float[] embedding,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a single transcript segment
    /// </summary>
    /// <param name="segment">The segment to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The added segment</returns>
    Task<TranscriptSegment> AddAsync(TranscriptSegment segment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple transcript segments
    /// </summary>
    /// <param name="segments">The segments to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The added segments</returns>
    Task<List<TranscriptSegment>> AddRangeAsync(
        List<TranscriptSegment> segments,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a transcript segment
    /// </summary>
    /// <param name="segment">The segment to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated segment</returns>
    Task<TranscriptSegment> UpdateAsync(TranscriptSegment segment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all segments for a video
    /// </summary>
    /// <param name="videoId">The video ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of segments deleted</returns>
    Task<int> DeleteByVideoIdAsync(string videoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets segments by time range for a video
    /// </summary>
    /// <param name="videoId">The video ID</param>
    /// <param name="startTime">Start time in seconds</param>
    /// <param name="endTime">End time in seconds</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Segments within the time range</returns>
    Task<List<TranscriptSegment>> GetByTimeRangeAsync(
        string videoId,
        double startTime,
        double endTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches segments by text content
    /// </summary>
    /// <param name="videoId">The video ID (optional)</param>
    /// <param name="searchText">Text to search for</param>
    /// <param name="limit">Maximum number of results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Matching segments</returns>
    Task<List<TranscriptSegment>> SearchByTextAsync(
        string? videoId,
        string searchText,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of segments for a video
    /// </summary>
    /// <param name="videoId">The video ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total count of segments</returns>
    Task<int> GetCountByVideoIdAsync(string videoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of segments with embeddings for a video
    /// </summary>
    /// <param name="videoId">The video ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of segments with embeddings</returns>
    Task<int> GetEmbeddingCountByVideoIdAsync(string videoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves changes to the database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of entities affected</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all transcript segments (generic repository compatibility)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All transcript segments</returns>
    Task<List<TranscriptSegment>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds transcript segments by predicate (generic repository compatibility)
    /// </summary>
    /// <param name="predicate">The filter predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Matching segments</returns>
    Task<List<TranscriptSegment>> FindAsync(
        System.Linq.Expressions.Expression<Func<TranscriptSegment, bool>> predicate,
        CancellationToken cancellationToken = default);
}