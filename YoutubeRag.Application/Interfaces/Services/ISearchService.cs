using YoutubeRag.Application.DTOs.Search;

namespace YoutubeRag.Application.Interfaces.Services;

/// <summary>
/// Service interface for semantic search operations
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Search across all videos and transcripts
    /// </summary>
    Task<SearchResponseDto> SearchAsync(SearchRequestDto searchDto);

    /// <summary>
    /// Search within a specific video's transcripts
    /// </summary>
    Task<SearchResponseDto> SearchByVideoAsync(string videoId, SearchRequestDto searchDto);

    /// <summary>
    /// Get similar videos based on a video ID
    /// </summary>
    Task<List<SearchResultDto>> GetSimilarVideosAsync(string videoId, int limit = 10);

    /// <summary>
    /// Get search suggestions based on partial query
    /// </summary>
    Task<List<string>> GetSearchSuggestionsAsync(string partialQuery, int limit = 5);
}
