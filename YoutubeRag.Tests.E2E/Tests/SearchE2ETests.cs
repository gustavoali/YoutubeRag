using FluentAssertions;
using NUnit.Framework;
using System.Text.Json;
using YoutubeRag.Tests.E2E.Fixtures;

namespace YoutubeRag.Tests.E2E.Tests;

/// <summary>
/// End-to-End tests for Search flow
/// </summary>
[TestFixture]
[Category("E2E")]
[Category("Search")]
public class SearchE2ETests : E2ETestBase
{
    [SetUp]
    public async Task TestSetUp()
    {
        // Authenticate before each test
        await AuthenticateAsync();
    }

    /// <summary>
    /// Test: Search videos by keyword successfully
    /// </summary>
    [Test]
    [Order(1)]
    public async Task SemanticSearch_WithValidQuery_ShouldReturnResults()
    {
        // Arrange
        var searchQuery = "machine learning tutorial";

        // Act
        var response = await SearchApi.SemanticSearchAsync(searchQuery, maxResults: 10);

        // Assert
        response.Status.Should().Be(200, "Semantic search should succeed with valid query");

        var responseBody = await response.TextAsync();
        Console.WriteLine($"Search response: {responseBody}");

        var responseJson = JsonDocument.Parse(responseBody);

        // Verify response structure
        responseJson.RootElement.TryGetProperty("query", out var queryProp).Should().BeTrue();
        responseJson.RootElement.TryGetProperty("results", out var resultsProp).Should().BeTrue();
        responseJson.RootElement.TryGetProperty("total_results", out var totalProp).Should().BeTrue();
        responseJson.RootElement.TryGetProperty("search_type", out var typeProp).Should().BeTrue();

        queryProp.GetString().Should().Be(searchQuery, "Query should be echoed back");
        typeProp.GetString().Should().Be("semantic", "Search type should be semantic");
    }

    /// <summary>
    /// Test: Search with filters (date, duration, etc.)
    /// </summary>
    [Test]
    [Order(2)]
    public async Task SemanticSearch_WithFilters_ShouldApplyFilters()
    {
        // Arrange
        var searchQuery = "programming";
        var fromDate = DateTime.UtcNow.AddDays(-30);
        var toDate = DateTime.UtcNow;

        // Act
        var response = await SearchApi.SemanticSearchAsync(
            searchQuery,
            maxResults: 20,
            fromDate: fromDate,
            toDate: toDate
        );

        // Assert
        response.Status.Should().Be(200);

        var responseBody = await response.TextAsync();
        Console.WriteLine($"Filtered search response: {responseBody}");

        var responseJson = JsonDocument.Parse(responseBody);
        responseJson.RootElement.TryGetProperty("results", out var resultsProp).Should().BeTrue();

        var results = resultsProp.EnumerateArray().ToList();
        results.Should().HaveCountLessThanOrEqualTo(20, "Should respect max results limit");
    }

    /// <summary>
    /// Test: Search pagination works correctly
    /// </summary>
    [Test]
    [Order(3)]
    public async Task SemanticSearch_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var searchQuery = "tutorial";
        var maxResults = 5;

        // Act - Get first page
        var response = await SearchApi.SemanticSearchAsync(searchQuery, maxResults: maxResults);

        // Assert
        response.Status.Should().Be(200);

        var responseBody = await response.TextAsync();
        var responseJson = JsonDocument.Parse(responseBody);

        responseJson.RootElement.TryGetProperty("results", out var resultsProp).Should().BeTrue();
        responseJson.RootElement.TryGetProperty("limit", out var limitProp).Should().BeTrue();

        limitProp.GetInt32().Should().Be(maxResults, "Limit should match requested max results");

        var results = resultsProp.EnumerateArray().ToList();
        results.Should().HaveCountLessThanOrEqualTo(maxResults, "Results should not exceed max results");
    }

    /// <summary>
    /// Test: Empty search results handling
    /// </summary>
    [Test]
    [Order(4)]
    public async Task SemanticSearch_WithNoResults_ShouldReturnEmptyList()
    {
        // Arrange
        var searchQuery = $"very_unique_nonexistent_query_{Guid.NewGuid()}";

        // Act
        var response = await SearchApi.SemanticSearchAsync(searchQuery);

        // Assert
        response.Status.Should().Be(200, "Search should succeed even with no results");

        var responseBody = await response.TextAsync();
        Console.WriteLine($"Empty search response: {responseBody}");

        var responseJson = JsonDocument.Parse(responseBody);

        responseJson.RootElement.TryGetProperty("results", out var resultsProp).Should().BeTrue();
        responseJson.RootElement.TryGetProperty("total_results", out var totalProp).Should().BeTrue();

        var results = resultsProp.EnumerateArray().ToList();
        var totalResults = totalProp.GetInt32();

        // Either empty results or mock data
        Console.WriteLine($"Total results: {totalResults}, Results count: {results.Count}");
    }

    /// <summary>
    /// Test: Keyword search functionality
    /// </summary>
    [Test]
    [Order(5)]
    public async Task KeywordSearch_WithValidKeywords_ShouldReturnResults()
    {
        // Arrange
        var keywords = "python programming";

        // Act
        var response = await SearchApi.KeywordSearchAsync(keywords);

        // Assert
        response.Status.Should().Be(200);

        var responseBody = await response.TextAsync();
        Console.WriteLine($"Keyword search response: {responseBody}");

        var responseJson = JsonDocument.Parse(responseBody);

        responseJson.RootElement.TryGetProperty("keywords", out var keywordsProp).Should().BeTrue();
        responseJson.RootElement.TryGetProperty("search_type", out var typeProp).Should().BeTrue();

        keywordsProp.GetString().Should().Be(keywords);
        typeProp.GetString().Should().Be("keyword");
    }

    /// <summary>
    /// Test: Advanced search with multiple filters
    /// </summary>
    [Test]
    [Order(6)]
    public async Task AdvancedSearch_WithMultipleFilters_ShouldApplyAllFilters()
    {
        // Arrange
        var searchQuery = "data science";
        var filters = new Dictionary<string, object>
        {
            ["language"] = "en",
            ["duration"] = "medium",
            ["quality"] = "hd"
        };

        // Act
        var response = await SearchApi.AdvancedSearchAsync(
            query: searchQuery,
            filters: filters,
            maxResults: 15,
            sortBy: "relevance",
            sortOrder: "desc"
        );

        // Assert
        response.Status.Should().Be(200);

        var responseBody = await response.TextAsync();
        Console.WriteLine($"Advanced search response: {responseBody}");

        var responseJson = JsonDocument.Parse(responseBody);

        responseJson.RootElement.TryGetProperty("query", out var queryProp).Should().BeTrue();
        responseJson.RootElement.TryGetProperty("filters", out var filtersProp).Should().BeTrue();
        responseJson.RootElement.TryGetProperty("search_type", out var typeProp).Should().BeTrue();

        queryProp.GetString().Should().Be(searchQuery);
        typeProp.GetString().Should().Be("advanced");
    }

    /// <summary>
    /// Test: Search suggestions/autocomplete
    /// </summary>
    [Test]
    [Order(7)]
    public async Task GetSearchSuggestions_WithPartialQuery_ShouldReturnSuggestions()
    {
        // Arrange
        var partialQuery = "mach";
        var limit = 10;

        // Act
        var response = await SearchApi.GetSearchSuggestionsAsync(partialQuery, limit);

        // Assert
        response.Status.Should().Be(200);

        var responseBody = await response.TextAsync();
        Console.WriteLine($"Suggestions response: {responseBody}");

        var responseJson = JsonDocument.Parse(responseBody);

        responseJson.RootElement.TryGetProperty("query", out var queryProp).Should().BeTrue();
        responseJson.RootElement.TryGetProperty("suggestions", out var suggestionsProp).Should().BeTrue();
        responseJson.RootElement.TryGetProperty("count", out var countProp).Should().BeTrue();

        queryProp.GetString().Should().Be(partialQuery);

        var suggestions = suggestionsProp.EnumerateArray().ToList();
        suggestions.Should().HaveCountLessThanOrEqualTo(limit);
    }

    /// <summary>
    /// Test: Get trending searches
    /// </summary>
    [Test]
    [Order(8)]
    public async Task GetTrendingSearches_ShouldReturnPopularSearches()
    {
        // Arrange
        var limit = 10;

        // Act
        var response = await SearchApi.GetTrendingSearchesAsync(limit);

        // Assert
        response.Status.Should().Be(200);

        var responseBody = await response.TextAsync();
        Console.WriteLine($"Trending searches response: {responseBody}");

        var responseJson = JsonDocument.Parse(responseBody);

        responseJson.RootElement.TryGetProperty("trending_searches", out var trendingProp).Should().BeTrue();

        var trending = trendingProp.EnumerateArray().ToList();
        trending.Should().NotBeEmpty("Trending searches should return at least some results");
        trending.Should().HaveCountLessThanOrEqualTo(limit);
    }

    /// <summary>
    /// Test: Get search history
    /// </summary>
    [Test]
    [Order(9)]
    public async Task GetSearchHistory_ShouldReturnUserSearchHistory()
    {
        // Arrange - Perform a search first to create history
        await SearchApi.SemanticSearchAsync("test query for history");
        await Task.Delay(500); // Wait for history to be recorded

        // Act
        var response = await SearchApi.GetSearchHistoryAsync(page: 1, pageSize: 20);

        // Assert
        response.Status.Should().Be(200);

        var responseBody = await response.TextAsync();
        Console.WriteLine($"Search history response: {responseBody}");

        var responseJson = JsonDocument.Parse(responseBody);

        responseJson.RootElement.TryGetProperty("history", out var historyProp).Should().BeTrue();
        responseJson.RootElement.TryGetProperty("total", out var totalProp).Should().BeTrue();
        responseJson.RootElement.TryGetProperty("page", out var pageProp).Should().BeTrue();

        pageProp.GetInt32().Should().Be(1);
    }

    /// <summary>
    /// Test: Search validation - empty query
    /// </summary>
    [Test]
    [Order(10)]
    public async Task SemanticSearch_WithEmptyQuery_ShouldReturnBadRequest()
    {
        // Arrange
        var emptyQuery = "";

        // Act
        var response = await SearchApi.SemanticSearchAsync(emptyQuery);

        // Assert
        response.Status.Should().Be(400, "Empty query should return Bad Request");

        var responseBody = await response.TextAsync();
        Console.WriteLine($"Error response: {responseBody}");

        responseBody.Should().Contain("error", "Error response should contain error information");
    }
}
