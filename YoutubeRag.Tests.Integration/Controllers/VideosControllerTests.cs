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
/// Integration tests for VideosController
/// </summary>
public class VideosControllerTests : IntegrationTestBase
{
    private readonly string _baseUrl = "/api/v1/videos";

    public VideosControllerTests(CustomWebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    protected override async Task SeedTestData()
    {
        // Seed some test videos for the authenticated user
        var userId = "test-user-id";
        var videos = new List<Video>
        {
            TestDataGenerator.GenerateVideo(userId, "video-1"),
            TestDataGenerator.GenerateVideo(userId, "video-2"),
            TestDataGenerator.GenerateVideo(userId, "video-3")
        };

        videos[0].Title = "Test Video 1";
        videos[0].Status = VideoStatus.Completed;
        videos[1].Title = "Test Video 2";
        videos[1].Status = VideoStatus.Processing;
        videos[2].Title = "Test Video 3";
        videos[2].Status = VideoStatus.Failed;

        await DbContext.Videos.AddRangeAsync(videos);
        await DbContext.SaveChangesAsync();
    }

    #region List Videos Tests

    /// <summary>
    /// Test listing videos with authentication
    /// </summary>
    [Fact]
    public async Task ListVideos_WhenAuthenticated_ReturnsVideosList()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync($"{_baseUrl}?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        dynamic result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("videos").GetArrayLength().Should().BeGreaterThanOrEqualTo(0);
        result.GetProperty("page").GetInt32().Should().Be(1);
        result.GetProperty("page_size").GetInt32().Should().Be(10);
    }

    /// <summary>
    /// Test listing videos without authentication
    /// </summary>
    [Fact]
    public async Task ListVideos_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"{_baseUrl}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Test pagination of videos
    /// </summary>
    [Fact]
    public async Task ListVideos_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        await AuthenticateAsync();

        // Seed more videos for pagination test
        var userId = "test-user-id";
        var videos = Enumerable.Range(1, 25)
            .Select(i => TestDataGenerator.GenerateVideo(userId, $"page-video-{i}"))
            .ToList();

        await DbContext.Videos.AddRangeAsync(videos);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"{_baseUrl}?page=2&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        dynamic result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("page").GetInt32().Should().Be(2);
        result.GetProperty("page_size").GetInt32().Should().Be(10);
        result.GetProperty("has_more").GetBoolean().Should().BeTrue();
    }

    #endregion

    #region Get Video Tests

    /// <summary>
    /// Test getting a specific video by ID
    /// </summary>
    [Fact]
    public async Task GetVideo_WithValidId_ReturnsVideo()
    {
        // Arrange
        await AuthenticateAsync();

        var userId = "test-user-id";
        var video = TestDataGenerator.GenerateVideo(userId, "get-video-test");
        video.Title = "Specific Video Test";

        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"{_baseUrl}/{video.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        dynamic result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("id").GetString().Should().Be(video.Id);
        result.GetProperty("title").GetString().Should().Be("Specific Video Test");
    }

    /// <summary>
    /// Test getting a non-existent video
    /// </summary>
    [Fact]
    public async Task GetVideo_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync($"{_baseUrl}/non-existent-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Test getting another user's video (authorization check)
    /// </summary>
    [Fact]
    public async Task GetVideo_OtherUsersVideo_ReturnsForbiddenOrNotFound()
    {
        // Arrange
        await AuthenticateAsync();

        var otherUserId = "other-user-id";
        var video = TestDataGenerator.GenerateVideo(otherUserId, "other-user-video");

        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"{_baseUrl}/{video.Id}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    #endregion

    #region Update Video Tests

    /// <summary>
    /// Test updating video metadata
    /// </summary>
    [Fact]
    public async Task UpdateVideo_WithValidData_ReturnsUpdatedVideo()
    {
        // Arrange
        await AuthenticateAsync();

        var userId = "test-user-id";
        var video = TestDataGenerator.GenerateVideo(userId, "update-video-test");
        video.Title = "Original Title";
        video.Description = "Original Description";

        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        var updateRequest = new
        {
            title = "Updated Title",
            description = "Updated Description"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"{_baseUrl}/{video.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        dynamic result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("title").GetString().Should().Be("Updated Title");
        result.GetProperty("description").GetString().Should().Be("Updated Description");
    }

    /// <summary>
    /// Test updating non-existent video
    /// </summary>
    [Fact]
    public async Task UpdateVideo_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsync();

        var updateRequest = new
        {
            title = "Updated Title"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"{_baseUrl}/non-existent-id", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Delete Video Tests

    /// <summary>
    /// Test deleting a video
    /// </summary>
    [Fact]
    public async Task DeleteVideo_WithValidId_ReturnsNoContent()
    {
        // Arrange
        await AuthenticateAsync();

        var userId = "test-user-id";
        var video = TestDataGenerator.GenerateVideo(userId, "delete-video-test");

        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.DeleteAsync($"{_baseUrl}/{video.Id}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.OK);

        // Verify video is deleted
        var checkResponse = await Client.GetAsync($"{_baseUrl}/{video.Id}");
        checkResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Test deleting non-existent video
    /// </summary>
    [Fact]
    public async Task DeleteVideo_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.DeleteAsync($"{_baseUrl}/non-existent-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Test deleting another user's video
    /// </summary>
    [Fact]
    public async Task DeleteVideo_OtherUsersVideo_ReturnsForbiddenOrNotFound()
    {
        // Arrange
        await AuthenticateAsync();

        var otherUserId = "other-user-id";
        var video = TestDataGenerator.GenerateVideo(otherUserId, "other-user-delete");

        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.DeleteAsync($"{_baseUrl}/{video.Id}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    #endregion

    #region Process Video from URL Tests

    /// <summary>
    /// Test processing video from YouTube URL
    /// </summary>
    [Fact]
    public async Task ProcessVideoFromUrl_WithValidYouTubeUrl_ReturnsAccepted()
    {
        // Arrange
        await AuthenticateAsync();

        var request = new
        {
            url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            title = "Test YouTube Video",
            description = "Test Description"
        };

        // Act
        var response = await Client.PostAsJsonAsync($"{_baseUrl}/from-url", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Accepted);

        var content = await response.Content.ReadAsStringAsync();
        dynamic result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
        result.GetProperty("status").GetString().Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Test processing video with invalid URL
    /// </summary>
    [Fact]
    public async Task ProcessVideoFromUrl_WithInvalidUrl_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsync();

        var request = new
        {
            url = "not-a-valid-url"
        };

        // Act
        var response = await Client.PostAsJsonAsync($"{_baseUrl}/from-url", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Test processing video without URL
    /// </summary>
    [Fact]
    public async Task ProcessVideoFromUrl_WithoutUrl_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsync();

        var request = new
        {
            title = "Test Video"
        };

        // Act
        var response = await Client.PostAsJsonAsync($"{_baseUrl}/from-url", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}