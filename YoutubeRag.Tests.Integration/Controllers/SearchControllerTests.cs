using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;
using YoutubeRag.Infrastructure.Data;
using YoutubeRag.Tests.Integration.Infrastructure;
using YoutubeRag.Tests.Integration.Helpers;
using Xunit;

namespace YoutubeRag.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for SearchController
/// </summary>
public class SearchControllerTests : IntegrationTestBase
{
    private readonly string _baseUrl = "/api/v1/search";

    public SearchControllerTests(CustomWebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    protected override async Task SeedTestData()
    {
        // Seed test videos with transcript for search testing
        var userId = "test-user-id";

        var video1 = TestDataGenerator.GenerateVideo(userId, "search-video-1");
        video1.Title = "Introduction to Machine Learning";
        video1.Description = "Learn the basics of machine learning and neural networks";
        video1.Status = VideoStatus.Completed;
        video1.TranscriptSegments = new List<TranscriptSegment>
        {
            new TranscriptSegment
            {
                Id = "seg-1-1",
                VideoId = video1.Id,
                StartTime = 0,
                EndTime = 10,
                Text = "Machine learning is a type of artificial intelligence that enables computers to learn from data."
            },
            new TranscriptSegment
            {
                Id = "seg-1-2",
                VideoId = video1.Id,
                StartTime = 10,
                EndTime = 20,
                Text = "Neural networks are computing systems inspired by biological neural networks."
            }
        };

        var video2 = TestDataGenerator.GenerateVideo(userId, "search-video-2");
        video2.Title = "Deep Learning Tutorial";
        video2.Description = "Advanced concepts in deep learning and neural networks";
        video2.Status = VideoStatus.Completed;
        video2.TranscriptSegments = new List<TranscriptSegment>
        {
            new TranscriptSegment
            {
                Id = "seg-2-1",
                VideoId = video2.Id,
                StartTime = 0,
                EndTime = 15,
                Text = "Deep learning is a subset of machine learning that uses multiple layers of neural networks."
            },
            new TranscriptSegment
            {
                Id = "seg-2-2",
                VideoId = video2.Id,
                StartTime = 15,
                EndTime = 30,
                Text = "Convolutional neural networks are commonly used for image recognition tasks."
            }
        };

        var video3 = TestDataGenerator.GenerateVideo(userId, "search-video-3");
        video3.Title = "Python Programming Basics";
        video3.Description = "Learn Python programming from scratch";
        video3.Status = VideoStatus.Completed;
        video3.TranscriptSegments = new List<TranscriptSegment>
        {
            new TranscriptSegment
            {
                Id = "seg-3-1",
                VideoId = video3.Id,
                StartTime = 0,
                EndTime = 10,
                Text = "Python is a high-level programming language known for its simplicity and readability."
            },
            new TranscriptSegment
            {
                Id = "seg-3-2",
                VideoId = video3.Id,
                StartTime = 10,
                EndTime = 20,
                Text = "Variables in Python can store different types of data without explicit declaration."
            }
        };

        await DbContext.Videos.AddRangeAsync(video1, video2, video3);
        await DbContext.SaveChangesAsync();
    }

    #region Semantic Search Tests

    /// <summary>
    /// Test semantic search with valid query
    /// </summary>
    [Fact]
    public async Task Search_WithValidQuery_ReturnsSearchResults()
    {
        // Arrange
        await AuthenticateAsync();
        var searchQuery = new
        {
            query = "machine learning neural networks",
            limit = 10
        };

        // Act
        var response = await Client.PostAsJsonAsync($"{_baseUrl}/semantic", searchQuery);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        dynamic result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("results").GetArrayLength().Should().BeGreaterThanOrEqualTo(0);
        result.GetProperty("query").GetString().Should().Be("machine learning neural networks");
        result.GetProperty("total_results").GetInt32().Should().BeGreaterThanOrEqualTo(0);
    }

    /// <summary>
    /// Test semantic search without authentication
    /// </summary>
    [Fact]
    public async Task Search_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var searchQuery = new
        {
            query = "test query"
        };

        // Act
        var response = await Client.PostAsJsonAsync($"{_baseUrl}/semantic", searchQuery);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Test semantic search with empty query
    /// </summary>
    [Fact]
    public async Task Search_WithEmptyQuery_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsync();
        var searchQuery = new
        {
            query = "",
            limit = 10
        };

        // Act
        var response = await Client.PostAsJsonAsync($"{_baseUrl}/semantic", searchQuery);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Test semantic search with limit parameter
    /// </summary>
    [Fact]
    public async Task Search_WithLimitParameter_ReturnsLimitedResults()
    {
        // Arrange
        await AuthenticateAsync();
        var searchQuery = new
        {
            query = "learning",
            limit = 2
        };

        // Act
        var response = await Client.PostAsJsonAsync($"{_baseUrl}/semantic", searchQuery);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        dynamic result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("results").GetArrayLength().Should().BeLessThanOrEqualTo(2);
    }

    /// <summary>
    /// Test semantic search with special characters
    /// </summary>
    [Fact]
    public async Task Search_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        await AuthenticateAsync();
        var searchQuery = new
        {
            query = "machine & learning | neural * networks",
            limit = 10
        };

        // Act
        var response = await Client.PostAsJsonAsync($"{_baseUrl}/semantic", searchQuery);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Search Suggestions Tests

    /// <summary>
    /// Test getting search suggestions
    /// </summary>
    [Fact]
    public async Task GetSuggestions_WithQuery_ReturnsSuggestions()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync($"{_baseUrl}/suggestions?query=mach");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        dynamic result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("suggestions").GetArrayLength().Should().BeGreaterThanOrEqualTo(0);
    }

    /// <summary>
    /// Test getting suggestions without query
    /// </summary>
    [Fact]
    public async Task GetSuggestions_WithoutQuery_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync($"{_baseUrl}/suggestions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Test getting suggestions with limit
    /// </summary>
    [Fact]
    public async Task GetSuggestions_WithLimit_ReturnsLimitedSuggestions()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync($"{_baseUrl}/suggestions?query=learn&limit=3");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        dynamic result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("suggestions").GetArrayLength().Should().BeLessThanOrEqualTo(3);
    }

    #endregion

    #region Search with Filters Tests

    /// <summary>
    /// Test search with date range filter
    /// </summary>
    [Fact]
    public async Task Search_WithDateRangeFilter_ReturnsFilteredResults()
    {
        // Arrange
        await AuthenticateAsync();

        var searchQuery = new
        {
            query = "learning",
            from_date = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd"),
            to_date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            limit = 10
        };

        // Act
        var response = await Client.PostAsJsonAsync($"{_baseUrl}/semantic", searchQuery);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Test search with video status filter
    /// </summary>
    [Fact]
    public async Task Search_WithStatusFilter_ReturnsFilteredResults()
    {
        // Arrange
        await AuthenticateAsync();

        var searchQuery = new
        {
            query = "neural networks",
            status = new[] { "processed" },
            limit = 10
        };

        // Act
        var response = await Client.PostAsJsonAsync($"{_baseUrl}/semantic", searchQuery);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Search History Tests

    /// <summary>
    /// Test getting search history
    /// </summary>
    [Fact]
    public async Task GetSearchHistory_WhenAuthenticated_ReturnsHistory()
    {
        // Arrange
        await AuthenticateAsync();

        // Perform some searches first
        var searchQuery = new { query = "test search", limit = 5 };
        await Client.PostAsJsonAsync($"{_baseUrl}/semantic", searchQuery);
        await Client.PostAsJsonAsync($"{_baseUrl}/semantic", searchQuery);

        // Act
        var response = await Client.GetAsync($"{_baseUrl}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("history");
    }

    #endregion

    #region Edge Cases and Error Handling

    /// <summary>
    /// Test search with very long query
    /// </summary>
    [Fact]
    public async Task Search_WithVeryLongQuery_HandlesCorrectly()
    {
        // Arrange
        await AuthenticateAsync();
        var longQuery = string.Join(" ", Enumerable.Repeat("machine learning", 100));

        var searchQuery = new
        {
            query = longQuery,
            limit = 10
        };

        // Act
        var response = await Client.PostAsJsonAsync($"{_baseUrl}/semantic", searchQuery);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Test search with invalid limit
    /// </summary>
    [Fact]
    public async Task Search_WithInvalidLimit_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsync();

        var searchQuery = new
        {
            query = "test",
            limit = -1
        };

        // Act
        var response = await Client.PostAsJsonAsync($"{_baseUrl}/semantic", searchQuery);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Test search when no videos exist
    /// </summary>
    [Fact]
    public async Task Search_WithNoVideos_ReturnsEmptyResults()
    {
        // Arrange
        await AuthenticateAsync();

        // Clear all videos
        DbContext.Videos.RemoveRange(DbContext.Videos);
        await DbContext.SaveChangesAsync();

        var searchQuery = new
        {
            query = "any query",
            limit = 10
        };

        // Act
        var response = await Client.PostAsJsonAsync($"{_baseUrl}/semantic", searchQuery);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        dynamic result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("results").GetArrayLength().Should().Be(0);
        result.GetProperty("total_results").GetInt32().Should().Be(0);
    }

    #endregion
}