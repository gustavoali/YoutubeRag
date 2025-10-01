using YoutubeRag.Domain.Entities;

namespace YoutubeRag.Application.Interfaces;

/// <summary>
/// Repository interface for TranscriptSegment entity operations
/// </summary>
public interface ITranscriptSegmentRepository : IRepository<TranscriptSegment>
{
    /// <summary>
    /// Gets all transcript segments for a specific video
    /// </summary>
    /// <param name="videoId">The video's unique identifier</param>
    /// <returns>A collection of transcript segments ordered by segment index</returns>
    Task<IEnumerable<TranscriptSegment>> GetByVideoIdAsync(string videoId);

    /// <summary>
    /// Searches transcript segments by text content
    /// </summary>
    /// <param name="searchText">The text to search for in transcript content</param>
    /// <returns>A collection of matching transcript segments</returns>
    Task<IEnumerable<TranscriptSegment>> SearchByTextAsync(string searchText);

    /// <summary>
    /// Searches transcript segments by text within a specific video
    /// </summary>
    /// <param name="videoId">The video's unique identifier</param>
    /// <param name="searchText">The text to search for in transcript content</param>
    /// <returns>A collection of matching transcript segments from the specified video</returns>
    Task<IEnumerable<TranscriptSegment>> SearchByTextInVideoAsync(string videoId, string searchText);

    /// <summary>
    /// Gets transcript segments for a video within a time range
    /// </summary>
    /// <param name="videoId">The video's unique identifier</param>
    /// <param name="startTime">The start time in seconds</param>
    /// <param name="endTime">The end time in seconds</param>
    /// <returns>A collection of transcript segments within the time range</returns>
    Task<IEnumerable<TranscriptSegment>> GetByVideoIdAndTimeRangeAsync(string videoId, double startTime, double endTime);

    /// <summary>
    /// Gets transcript segments by language
    /// </summary>
    /// <param name="videoId">The video's unique identifier</param>
    /// <param name="language">The language code (e.g., "en", "es")</param>
    /// <returns>A collection of transcript segments in the specified language</returns>
    Task<IEnumerable<TranscriptSegment>> GetByVideoIdAndLanguageAsync(string videoId, string language);

    /// <summary>
    /// Gets paginated transcript segments for a video
    /// </summary>
    /// <param name="videoId">The video's unique identifier</param>
    /// <param name="pageNumber">The page number (1-based)</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <returns>A collection of paginated transcript segments</returns>
    Task<IEnumerable<TranscriptSegment>> GetPaginatedByVideoIdAsync(string videoId, int pageNumber, int pageSize);

    /// <summary>
    /// Gets a specific segment by video ID and segment index
    /// </summary>
    /// <param name="videoId">The video's unique identifier</param>
    /// <param name="segmentIndex">The segment index</param>
    /// <returns>The transcript segment if found; otherwise, null</returns>
    Task<TranscriptSegment?> GetByVideoIdAndIndexAsync(string videoId, int segmentIndex);

    /// <summary>
    /// Deletes all transcript segments for a video
    /// </summary>
    /// <param name="videoId">The video's unique identifier</param>
    /// <returns>The number of deleted segments</returns>
    Task<int> DeleteByVideoIdAsync(string videoId);

    /// <summary>
    /// Gets the total duration of a video based on its transcript segments
    /// </summary>
    /// <param name="videoId">The video's unique identifier</param>
    /// <returns>The total duration in seconds</returns>
    Task<double> GetVideoDurationAsync(string videoId);

    /// <summary>
    /// Searches for similar segments using embedding similarity
    /// </summary>
    /// <param name="embedding">The embedding vector to compare against</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <param name="threshold">Similarity threshold (0-1)</param>
    /// <returns>A collection of similar transcript segments</returns>
    Task<IEnumerable<TranscriptSegment>> SearchBySimilarityAsync(float[] embedding, int limit = 10, float threshold = 0.7f);

    /// <summary>
    /// Gets transcript segments that have embeddings
    /// </summary>
    /// <param name="videoId">The video's unique identifier</param>
    /// <returns>A collection of transcript segments with embeddings</returns>
    Task<IEnumerable<TranscriptSegment>> GetWithEmbeddingsByVideoIdAsync(string videoId);
}