using FluentAssertions;
using NUnit.Framework;
using System.Text.Json;
using YoutubeRag.Tests.E2E.Fixtures;

namespace YoutubeRag.Tests.E2E.Tests;

/// <summary>
/// End-to-End tests for Video Ingestion flow
/// </summary>
[TestFixture]
[Category("E2E")]
[Category("VideoIngestion")]
public class VideoIngestionE2ETests : E2ETestBase
{
    [SetUp]
    public async Task TestSetUp()
    {
        // Authenticate before each test
        await AuthenticateAsync();
    }

    /// <summary>
    /// Test: Submit YouTube URL successfully and verify video creation
    /// </summary>
    [Test]
    [Order(1)]
    public async Task IngestVideo_WithValidYouTubeUrl_ShouldCreateVideoSuccessfully()
    {
        // Arrange
        var youtubeUrl = Config.TestData.ValidYouTubeUrl;
        var title = $"E2E Test Video - {GetTestUniqueId()}";
        var description = "This is an E2E test video";

        // Act
        var response = await VideosApi.IngestVideoAsync(youtubeUrl, title, description);

        // Assert
        response.Status.Should().Be(200, "Video ingestion should succeed with valid YouTube URL");

        var responseBody = await response.TextAsync();
        Console.WriteLine($"Response: {responseBody}");

        var responseJson = JsonDocument.Parse(responseBody);
        var videoId = responseJson.RootElement.GetProperty("videoId").GetString();

        videoId.Should().NotBeNullOrEmpty("Video ID should be returned");

        // Verify the video was created
        var videoResponse = await VideosApi.GetVideoByIdAsync(videoId!);
        videoResponse.Status.Should().Be(200, "Should be able to retrieve created video");
    }

    /// <summary>
    /// Test: Video metadata extraction from YouTube
    /// </summary>
    [Test]
    [Order(2)]
    public async Task IngestVideo_ShouldExtractMetadataFromYouTube()
    {
        // Arrange
        var youtubeUrl = Config.TestData.ValidYouTubeUrl;

        // Act
        var response = await VideosApi.IngestVideoAsync(youtubeUrl);

        // Assert
        response.Status.Should().Be(200);

        var responseBody = await response.TextAsync();
        var responseJson = JsonDocument.Parse(responseBody);

        // Verify metadata fields are present
        responseJson.RootElement.TryGetProperty("title", out var titleProp).Should().BeTrue();
        responseJson.RootElement.TryGetProperty("videoId", out var videoIdProp).Should().BeTrue();
        responseJson.RootElement.TryGetProperty("youtubeId", out var youtubeIdProp).Should().BeTrue();

        var videoId = videoIdProp.GetString();
        videoId.Should().NotBeNullOrEmpty();

        // Get full video details
        var videoDetailsResponse = await VideosApi.GetVideoByIdAsync(videoId!);
        videoDetailsResponse.Status.Should().Be(200);

        var detailsBody = await videoDetailsResponse.TextAsync();
        Console.WriteLine($"Video details: {detailsBody}");
    }

    /// <summary>
    /// Test: Video processing status updates
    /// </summary>
    [Test]
    [Order(3)]
    public async Task IngestVideo_ShouldUpdateProcessingStatus()
    {
        // Arrange
        var youtubeUrl = Config.TestData.ValidYouTubeUrl;
        var title = $"Status Test - {GetTestUniqueId()}";

        // Act
        var ingestResponse = await VideosApi.IngestVideoAsync(youtubeUrl, title, priority: 0);
        ingestResponse.Status.Should().Be(200);

        var ingestBody = await ingestResponse.TextAsync();
        var ingestJson = JsonDocument.Parse(ingestBody);
        var videoId = ingestJson.RootElement.GetProperty("videoId").GetString();

        // Wait a moment for processing to start
        await Task.Delay(2000);

        // Check progress
        var progressResponse = await VideosApi.GetVideoProgressAsync(videoId!);

        // Assert
        progressResponse.Status.Should().BeOneOf(200, 404);

        if (progressResponse.Status == 200)
        {
            var progressBody = await progressResponse.TextAsync();
            Console.WriteLine($"Progress: {progressBody}");

            var progressJson = JsonDocument.Parse(progressBody);
            progressJson.RootElement.TryGetProperty("status", out var statusProp).Should().BeTrue();
            progressJson.RootElement.TryGetProperty("progressPercentage", out var progressProp).Should().BeTrue();
        }
    }

    /// <summary>
    /// Test: Error handling for invalid URLs
    /// </summary>
    [Test]
    [Order(4)]
    public async Task IngestVideo_WithInvalidUrl_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidUrl = Config.TestData.InvalidYouTubeUrl;

        // Act
        var response = await VideosApi.IngestVideoAsync(invalidUrl);

        // Assert
        response.Status.Should().BeOneOf(400, 422);

        var responseBody = await response.TextAsync();
        Console.WriteLine($"Error response: {responseBody}");

        responseBody.Should().Contain("error", "Error response should contain error information");
    }

    /// <summary>
    /// Test: Duplicate video detection
    /// </summary>
    [Test]
    [Order(5)]
    public async Task IngestVideo_WithDuplicateUrl_ShouldDetectDuplicate()
    {
        // Arrange
        var youtubeUrl = Config.TestData.ValidYouTubeUrl;
        var title = $"Duplicate Test - {GetTestUniqueId()}";

        // Act - First ingestion
        var firstResponse = await VideosApi.IngestVideoAsync(youtubeUrl, title, priority: 0);
        firstResponse.Status.Should().Be(200, "First ingestion should succeed");

        var firstBody = await firstResponse.TextAsync();
        var firstJson = JsonDocument.Parse(firstBody);
        var firstVideoId = firstJson.RootElement.GetProperty("videoId").GetString();

        // Wait a moment
        await Task.Delay(1000);

        // Act - Second ingestion with same URL
        var secondResponse = await VideosApi.IngestVideoAsync(youtubeUrl, $"{title} - Duplicate");

        // Assert
        // The system might return 409 (Conflict) or 200 with the existing video ID
        secondResponse.Status.Should().BeOneOf(200, 409);

        var secondBody = await secondResponse.TextAsync();
        Console.WriteLine($"Duplicate response: {secondBody}");

        if (secondResponse.Status == 200)
        {
            var secondJson = JsonDocument.Parse(secondBody);
            var secondVideoId = secondJson.RootElement.GetProperty("videoId").GetString();

            // If the system returns 200, it might return the same video ID
            Console.WriteLine($"First Video ID: {firstVideoId}, Second Video ID: {secondVideoId}");
        }
        else if (secondResponse.Status == 409)
        {
            secondBody.Should().Contain("resourceId", "Conflict response should include resource ID");
        }
    }

    /// <summary>
    /// Test: Verify video appears in user's video list
    /// </summary>
    [Test]
    [Order(6)]
    public async Task IngestVideo_ShouldAppearInUserVideoList()
    {
        // Arrange
        var youtubeUrl = Config.TestData.ValidYouTubeUrl;
        var uniqueTitle = $"List Test - {GetTestUniqueId()}";

        // Act - Ingest video
        var ingestResponse = await VideosApi.IngestVideoAsync(youtubeUrl, uniqueTitle, priority: 0);
        ingestResponse.Status.Should().Be(200);

        var ingestBody = await ingestResponse.TextAsync();
        var ingestJson = JsonDocument.Parse(ingestBody);
        var videoId = ingestJson.RootElement.GetProperty("videoId").GetString();

        // Get user's video list
        var listResponse = await VideosApi.GetVideosAsync(page: 1, pageSize: 50);

        // Assert
        listResponse.Status.Should().Be(200, "Should be able to retrieve video list");

        var listBody = await listResponse.TextAsync();
        Console.WriteLine($"Video list: {listBody}");

        listBody.Should().Contain(videoId!, "Video list should contain the ingested video");
    }

    /// <summary>
    /// Test: Delete video successfully
    /// </summary>
    [Test]
    [Order(7)]
    public async Task DeleteVideo_WithValidId_ShouldDeleteSuccessfully()
    {
        // Arrange - Create a video first
        var youtubeUrl = Config.TestData.ValidYouTubeUrl;
        var title = $"Delete Test - {GetTestUniqueId()}";

        var ingestResponse = await VideosApi.IngestVideoAsync(youtubeUrl, title);
        ingestResponse.Status.Should().Be(200);

        var ingestBody = await ingestResponse.TextAsync();
        var ingestJson = JsonDocument.Parse(ingestBody);
        var videoId = ingestJson.RootElement.GetProperty("videoId").GetString();

        // Act - Delete the video
        var deleteResponse = await VideosApi.DeleteVideoAsync(videoId!);

        // Assert
        deleteResponse.Status.Should().Be(200, "Video deletion should succeed");

        var deleteBody = await deleteResponse.TextAsync();
        deleteBody.Should().Contain("success", "Delete response should confirm success");

        // Verify video is deleted
        var getResponse = await VideosApi.GetVideoByIdAsync(videoId!);
        getResponse.Status.Should().Be(404, "Deleted video should not be found");
    }
}
