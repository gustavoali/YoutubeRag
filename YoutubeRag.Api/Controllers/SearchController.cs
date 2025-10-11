using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using YoutubeRag.Api.Configuration;
using YoutubeRag.Application.DTOs.Search;
using YoutubeRag.Application.Exceptions;
using YoutubeRag.Application.Interfaces.Services;

namespace YoutubeRag.Api.Controllers;

[ApiController]
[Route("api/v1/search")]
[Tags("🔍 Search")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly AppSettings _appSettings;

    public SearchController(
        ISearchService searchService,
        IOptions<AppSettings> appSettings)
    {
        _searchService = searchService;
        _appSettings = appSettings.Value;
    }
    /// <summary>
    /// Semantic search across video transcripts
    /// </summary>
    [HttpPost("semantic")]
    public async Task<ActionResult> SemanticSearch([FromBody] SemanticSearchRequest request)
    {
        if (string.IsNullOrEmpty(request.Query))
        {
            return BadRequest(new { error = new { code = "VALIDATION_ERROR", message = "Query is required" } });
        }

        // Validate MaxResults (limit)
        if (request.MaxResults <= 0 || request.MaxResults > 100)
        {
            return BadRequest(new { error = new { code = "VALIDATION_ERROR", message = "MaxResults must be between 1 and 100" } });
        }

        try
        {
            var startTime = DateTime.UtcNow;

            var searchDto = new SearchRequestDto(
                Query: request.Query,
                Limit: request.MaxResults,
                Offset: 0,
                MinScore: request.MinRelevanceScore
            );

            var searchResults = await _searchService.SearchAsync(searchDto);

            var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            return Ok(new
            {
                query = searchResults.Query,
                results = searchResults.Results,
                total_results = searchResults.TotalResults,
                search_type = "semantic",
                processing_time_ms = Math.Round(processingTime, 1),
                mode = _appSettings.UseRealProcessing ? "real" : "mock",
                limit = searchResults.Limit,
                offset = searchResults.Offset
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "SEARCH_ERROR",
                    message = ex.Message
                }
            });
        }
    }

    /// <summary>
    /// Keyword-based search
    /// </summary>
    [HttpPost("keyword")]
    public async Task<ActionResult> KeywordSearch([FromBody] KeywordSearchRequest request)
    {
        if (string.IsNullOrEmpty(request.Keywords))
        {
            return BadRequest(new { error = new { code = "VALIDATION_ERROR", message = "Keywords are required" } });
        }

        // Mock keyword search results
        var results = new[]
        {
            new {
                video_id = "1",
                video_title = "Sample Video 1",
                matches = new[] {
                    new {
                        segment_id = "seg_1",
                        text = "Found keyword match in this segment",
                        start_time = 30.0,
                        end_time = 35.5
                    }
                }
            }
        };

        return Ok(new
        {
            keywords = request.Keywords,
            results,
            total_videos = results.Length,
            search_type = "keyword"
        });
    }

    /// <summary>
    /// Multi-modal search (text + filters)
    /// </summary>
    [HttpPost("advanced")]
    public async Task<ActionResult> AdvancedSearch([FromBody] AdvancedSearchRequest request)
    {
        // Mock advanced search with filters
        var results = new[]
        {
            new {
                video_id = "1",
                video_title = "Sample Video 1",
                segments = new[] {
                    new {
                        segment_id = "seg_1",
                        text = "Advanced search result with multiple criteria",
                        start_time = 60.0,
                        end_time = 67.2,
                        confidence = 0.95
                    }
                },
                match_score = 0.89
            }
        };

        return Ok(new
        {
            query = request.Query,
            filters = request.Filters,
            results,
            total_results = results.Length,
            search_type = "advanced"
        });
    }

    /// <summary>
    /// Get search suggestions/autocomplete
    /// </summary>
    [HttpGet("suggestions")]
    public async Task<ActionResult> GetSearchSuggestions(string? query, string? q, int limit = 10)
    {
        // Support both 'query' and 'q' parameters for compatibility
        var searchQuery = query ?? q;

        if (string.IsNullOrEmpty(searchQuery))
        {
            return BadRequest(new { error = new { code = "VALIDATION_ERROR", message = "Query parameter 'query' or 'q' is required" } });
        }

        // Validate limit parameter
        if (limit <= 0 || limit > 100)
        {
            return BadRequest(new { error = new { code = "VALIDATION_ERROR", message = "Limit must be between 1 and 100" } });
        }

        try
        {
            var suggestions = await _searchService.GetSearchSuggestionsAsync(searchQuery, limit);

            return Ok(new
            {
                query = searchQuery,
                suggestions,
                count = suggestions.Count
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR", message = ex.Message } });
        }
    }

    /// <summary>
    /// Get popular search terms
    /// </summary>
    [HttpGet("trending")]
    public async Task<ActionResult> GetTrendingSearches(int limit = 20)
    {
        var trending = new[]
        {
            new { term = "machine learning", count = 245 },
            new { term = "artificial intelligence", count = 189 },
            new { term = "data science", count = 167 },
            new { term = "python programming", count = 143 }
        }.Take(limit);

        return Ok(new
        {
            trending_searches = trending,
            period = "last_7_days",
            total_searches = trending.Sum(t => t.count)
        });
    }

    /// <summary>
    /// Get user's search history
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult> GetSearchHistory(int page = 1, int pageSize = 20)
    {
        try
        {
            // Mock search history data
            var searchHistory = new[]
            {
                new {
                    id = "search_hist_1",
                    query = "microservices architecture patterns",
                    search_type = "semantic",
                    results_count = 18,
                    timestamp = DateTime.UtcNow.AddHours(-1),
                    processing_time_ms = 312
                },
                new {
                    id = "search_hist_2",
                    query = "docker kubernetes deployment",
                    search_type = "advanced",
                    results_count = 24,
                    timestamp = DateTime.UtcNow.AddHours(-3),
                    processing_time_ms = 456
                },
                new {
                    id = "search_hist_3",
                    query = "react hooks tutorial",
                    search_type = "keyword",
                    results_count = 15,
                    timestamp = DateTime.UtcNow.AddHours(-5),
                    processing_time_ms = 189
                },
                new {
                    id = "search_hist_4",
                    query = "graphql vs rest api",
                    search_type = "semantic",
                    results_count = 22,
                    timestamp = DateTime.UtcNow.AddHours(-7),
                    processing_time_ms = 278
                },
                new {
                    id = "search_hist_5",
                    query = "typescript best practices",
                    search_type = "advanced",
                    results_count = 31,
                    timestamp = DateTime.UtcNow.AddDays(-1),
                    processing_time_ms = 523
                }
            };

            // Apply pagination
            var skip = (page - 1) * pageSize;
            var paginatedHistory = searchHistory.Skip(skip).Take(pageSize).ToArray();

            return Ok(new
            {
                history = paginatedHistory,
                total = searchHistory.Length,
                page = page,
                page_size = pageSize
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                error = new
                {
                    code = "INTERNAL_ERROR",
                    message = "Failed to retrieve search history: " + ex.Message
                }
            });
        }
    }
}

public class SemanticSearchRequest
{
    public string Query { get; set; } = string.Empty;

    // Support both 'MaxResults' and 'limit' for compatibility
    private int _maxResults = 10;
    public int MaxResults
    {
        get => _maxResults;
        set => _maxResults = value;
    }

    // Alternative property name for compatibility with tests
    public int Limit
    {
        get => _maxResults;
        set => _maxResults = value;
    }

    public double MinRelevanceScore { get; set; } = 0.5;
    public string[]? VideoIds { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class KeywordSearchRequest
{
    public string Keywords { get; set; } = string.Empty;
    public bool ExactMatch { get; set; } = false;
    public bool CaseSensitive { get; set; } = false;
    public string[]? VideoIds { get; set; }
}

public class AdvancedSearchRequest
{
    public string? Query { get; set; }
    public Dictionary<string, object>? Filters { get; set; }
    public int MaxResults { get; set; } = 20;
    public string SortBy { get; set; } = "relevance";
    public string SortOrder { get; set; } = "desc";
}
