using Microsoft.Playwright;

namespace YoutubeRag.Tests.E2E.PageObjects;

/// <summary>
/// Page Object for Search API endpoints
/// </summary>
public class SearchApi : ApiClient
{
    public SearchApi(IAPIRequestContext requestContext, string baseUrl)
        : base(requestContext, baseUrl)
    {
    }

    /// <summary>
    /// Perform semantic search
    /// </summary>
    public async Task<IAPIResponse> SemanticSearchAsync(
        string query,
        int maxResults = 10,
        double minRelevanceScore = 0.5,
        string[]? videoIds = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var data = new
        {
            query,
            maxResults,
            limit = maxResults, // Support both parameter names
            minRelevanceScore,
            videoIds,
            fromDate,
            toDate
        };

        return await PostAsync("/search/semantic", data);
    }

    /// <summary>
    /// Perform keyword search
    /// </summary>
    public async Task<IAPIResponse> KeywordSearchAsync(
        string keywords,
        bool exactMatch = false,
        bool caseSensitive = false,
        string[]? videoIds = null)
    {
        var data = new
        {
            keywords,
            exactMatch,
            caseSensitive,
            videoIds
        };

        return await PostAsync("/search/keyword", data);
    }

    /// <summary>
    /// Perform advanced search with filters
    /// </summary>
    public async Task<IAPIResponse> AdvancedSearchAsync(
        string? query = null,
        Dictionary<string, object>? filters = null,
        int maxResults = 20,
        string sortBy = "relevance",
        string sortOrder = "desc")
    {
        var data = new
        {
            query,
            filters,
            maxResults,
            sortBy,
            sortOrder
        };

        return await PostAsync("/search/advanced", data);
    }

    /// <summary>
    /// Get search suggestions/autocomplete
    /// </summary>
    public async Task<IAPIResponse> GetSearchSuggestionsAsync(string query, int limit = 10)
    {
        return await GetAsync($"/search/suggestions?q={Uri.EscapeDataString(query)}&limit={limit}");
    }

    /// <summary>
    /// Get trending searches
    /// </summary>
    public async Task<IAPIResponse> GetTrendingSearchesAsync(int limit = 20)
    {
        return await GetAsync($"/search/trending?limit={limit}");
    }

    /// <summary>
    /// Get search history
    /// </summary>
    public async Task<IAPIResponse> GetSearchHistoryAsync(int page = 1, int pageSize = 20)
    {
        return await GetAsync($"/search/history?page={page}&pageSize={pageSize}");
    }
}
