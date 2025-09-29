using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace YoutubeRag.Api.Controllers;

[ApiController]
[Route("api/v1/search")]
[Tags("🔍 Search")]
[Authorize]
public class SearchController : ControllerBase
{
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

        // Mock semantic search results
        var results = new[]
        {
            new {
                video_id = "1",
                video_title = "Sample Video 1",
                segment_id = "seg_1",
                segment_text = "This is a relevant segment that matches your query about YouTube RAG",
                start_time = 45.5,
                end_time = 52.3,
                relevance_score = 0.92,
                youtube_url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                thumbnail_url = "https://img.youtube.com/vi/dQw4w9WgXcQ/maxresdefault.jpg"
            },
            new {
                video_id = "2",
                video_title = "Sample Video 2",
                segment_id = "seg_2",
                segment_text = "Another relevant segment discussing video processing and analysis",
                start_time = 120.1,
                end_time = 128.7,
                relevance_score = 0.87,
                youtube_url = "https://www.youtube.com/watch?v=oHg5SJYRHA0",
                thumbnail_url = "https://img.youtube.com/vi/oHg5SJYRHA0/maxresdefault.jpg"
            }
        };

        return Ok(new {
            query = request.Query,
            results,
            total_results = results.Length,
            search_type = "semantic",
            processing_time_ms = 245
        });
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

        return Ok(new {
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

        return Ok(new {
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
    public async Task<ActionResult> GetSearchSuggestions(string q, int limit = 10)
    {
        if (string.IsNullOrEmpty(q))
        {
            return BadRequest(new { error = new { code = "VALIDATION_ERROR", message = "Query parameter 'q' is required" } });
        }

        // Mock search suggestions
        var suggestions = new[]
        {
            "YouTube API integration",
            "YouTube video processing",
            "YouTube transcript analysis",
            "YouTube data extraction"
        }.Take(limit);

        return Ok(new {
            query = q,
            suggestions,
            count = suggestions.Count()
        });
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

        return Ok(new {
            trending_searches = trending,
            period = "last_7_days",
            total_searches = trending.Sum(t => t.count)
        });
    }
}

public class SemanticSearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int MaxResults { get; set; } = 10;
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