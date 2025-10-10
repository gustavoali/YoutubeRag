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
/// Integration tests for UsersController
/// </summary>
public class UsersControllerTests : IntegrationTestBase
{
    private readonly string _baseUrl = "/api/v1/users";

    public UsersControllerTests(CustomWebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    protected override async Task SeedTestData()
    {
        // Note: This method runs BEFORE authentication in tests
        // So we don't seed here - tests will seed their own data after authenticating
        await Task.CompletedTask;
    }

    private async Task SeedUserTestData()
    {
        // Seed test data for user statistics
        var userId = AuthenticatedUserId;

        // Add some videos for the user
        var videos = new List<Video>
        {
            TestDataGenerator.GenerateVideo(userId, "user-video-1"),
            TestDataGenerator.GenerateVideo(userId, "user-video-2"),
            TestDataGenerator.GenerateVideo(userId, "user-video-3"),
            TestDataGenerator.GenerateVideo(userId, "user-video-4"),
            TestDataGenerator.GenerateVideo(userId, "user-video-5")
        };

        videos[0].Status = VideoStatus.Completed;
        videos[1].Status = VideoStatus.Completed;
        videos[2].Status = VideoStatus.Processing;
        videos[3].Status = VideoStatus.Failed;
        videos[4].Status = VideoStatus.Pending;

        // Add some jobs for the user
        var jobs = new List<Job>
        {
            TestDataGenerator.GenerateJob(userId, videos[0].Id),
            TestDataGenerator.GenerateJob(userId, videos[1].Id),
            TestDataGenerator.GenerateJob(userId, videos[2].Id)
        };

        jobs[0].Status = JobStatus.Completed;
        jobs[1].Status = JobStatus.Completed;
        jobs[2].Status = JobStatus.Running;

        await DbContext.Videos.AddRangeAsync(videos);
        await DbContext.Jobs.AddRangeAsync(jobs);
        await DbContext.SaveChangesAsync();
    }

    #region Get Current User Profile Tests

    /// <summary>
    /// Test getting current user profile when authenticated
    /// </summary>
    [Fact]
    public async Task GetCurrentUser_WhenAuthenticated_ReturnsUserProfile()
    {
        // Arrange
        var email = "profile@example.com";
        await AuthenticateAsync(email);

        // Act
        var response = await Client.GetAsync($"{_baseUrl}/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("email").GetString().Should().Be(email);
        result.GetProperty("name").GetString().Should().NotBeNullOrEmpty();
        result.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Test getting current user profile without authentication
    /// </summary>
    [Fact]
    public async Task GetCurrentUser_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"{_baseUrl}/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Update User Profile Tests

    /// <summary>
    /// Test updating user profile with valid data
    /// </summary>
    [Fact]
    public async Task UpdateProfile_WithValidData_ReturnsUpdatedProfile()
    {
        // Arrange
        await AuthenticateAsync();

        var updateRequest = new
        {
            name = "Updated User Name",
            preferences = new
            {
                language = "en",
                timezone = "UTC"
            }
        };

        // Act
        var response = await Client.PutAsJsonAsync($"{_baseUrl}/me", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("name").GetString().Should().Be("Updated User Name");
    }

    /// <summary>
    /// Test updating profile without authentication
    /// </summary>
    [Fact]
    public async Task UpdateProfile_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var updateRequest = new
        {
            name = "New Name"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"{_baseUrl}/me", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Test updating profile with invalid data
    /// </summary>
    [Fact]
    public async Task UpdateProfile_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsync();

        var updateRequest = new
        {
            name = "" // Empty name should be invalid
        };

        // Act
        var response = await Client.PutAsJsonAsync($"{_baseUrl}/me", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region User Statistics Tests

    /// <summary>
    /// Test getting user statistics
    /// </summary>
    [Fact]
    public async Task GetUserStats_WhenAuthenticated_ReturnsStatistics()
    {
        // Arrange
        await AuthenticateAsync();
        await SeedUserTestData();

        // Act
        var response = await Client.GetAsync($"{_baseUrl}/me/stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        // Check for expected statistics properties
        result.GetProperty("total_videos").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        result.GetProperty("processed_videos").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        result.GetProperty("total_watch_time").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        result.GetProperty("storage_used").GetInt64().Should().BeGreaterThanOrEqualTo(0);
    }

    /// <summary>
    /// Test getting user statistics without authentication
    /// </summary>
    [Fact]
    public async Task GetUserStats_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"{_baseUrl}/me/stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Test getting detailed statistics with time range
    /// </summary>
    [Fact]
    public async Task GetUserStats_WithTimeRange_ReturnsFilteredStatistics()
    {
        // Arrange
        await AuthenticateAsync();

        var fromDate = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
        var toDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // Act
        var response = await Client.GetAsync($"{_baseUrl}/me/stats?from={fromDate}&to={toDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("period").GetProperty("from").GetString().Should().NotBeNullOrEmpty();
        result.GetProperty("period").GetProperty("to").GetString().Should().NotBeNullOrEmpty();
    }

    #endregion

    #region User Preferences Tests

    /// <summary>
    /// Test getting user preferences
    /// </summary>
    [Fact]
    public async Task GetPreferences_WhenAuthenticated_ReturnsPreferences()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync($"{_baseUrl}/me/preferences");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("preferences");
    }

    /// <summary>
    /// Test updating user preferences
    /// </summary>
    [Fact]
    public async Task UpdatePreferences_WithValidData_ReturnsUpdatedPreferences()
    {
        // Arrange
        await AuthenticateAsync();

        var preferences = new
        {
            language = "es",
            theme = "dark",
            notifications = new
            {
                email = true,
                push = false
            }
        };

        // Act
        var response = await Client.PutAsJsonAsync($"{_baseUrl}/me/preferences", preferences);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("language").GetString().Should().Be("es");
        result.GetProperty("theme").GetString().Should().Be("dark");
    }

    #endregion

    #region User Activity Tests

    /// <summary>
    /// Test getting user activity history
    /// </summary>
    [Fact]
    public async Task GetActivityHistory_WhenAuthenticated_ReturnsActivity()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync($"{_baseUrl}/me/activity");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("activities").GetArrayLength().Should().BeGreaterThanOrEqualTo(0);
    }

    /// <summary>
    /// Test getting activity with pagination
    /// </summary>
    [Fact]
    public async Task GetActivityHistory_WithPagination_ReturnsPaginatedResults()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync($"{_baseUrl}/me/activity?page=1&pageSize=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("page").GetInt32().Should().Be(1);
        result.GetProperty("pageSize").GetInt32().Should().Be(5);
    }

    #endregion

    #region Delete Account Tests

    /// <summary>
    /// Test deleting user account
    /// </summary>
    [Fact]
    public async Task DeleteAccount_WithConfirmation_DeletesAccount()
    {
        // Arrange
        var email = "delete@example.com";
        await AuthenticateAsync(email);

        var deleteRequest = new
        {
            reason = "Test deletion",
            password = "Test123!",
            confirmation = "DELETE"
        };

        // Act
        var response = await Client.PostAsJsonAsync($"{_baseUrl}/me/delete", deleteRequest);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
    }

    /// <summary>
    /// Test deleting account without proper confirmation
    /// </summary>
    [Fact]
    public async Task DeleteAccount_WithoutConfirmation_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsync();

        var deleteRequest = new
        {
            reason = "", // Empty reason should trigger BadRequest
            password = "Test123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync($"{_baseUrl}/me/delete", deleteRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Export Data Tests

    /// <summary>
    /// Test exporting user data
    /// </summary>
    [Fact]
    public async Task ExportData_WhenAuthenticated_ReturnsDataExport()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync($"{_baseUrl}/me/export");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Check if response contains expected content type for download
        response.Content.Headers.ContentType?.MediaType.Should().BeOneOf(
            "application/json",
            "application/zip",
            "application/octet-stream"
        );
    }

    #endregion
}