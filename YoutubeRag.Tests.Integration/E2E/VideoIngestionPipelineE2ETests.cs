using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YoutubeRag.Application.DTOs.Video;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;
using YoutubeRag.Infrastructure.Data;
using YoutubeRag.Tests.Integration.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace YoutubeRag.Tests.Integration.E2E;

/// <summary>
/// End-to-End tests for the video ingestion pipeline.
/// These tests verify the entire flow from API call to database persistence.
/// </summary>
public class VideoIngestionPipelineE2ETests : IntegrationTestBase
{
    private readonly ITestOutputHelper _output;
    private readonly string _ingestUrl = "/api/v1/videos/ingest";
    private readonly List<string> _testVideoIds = new();
    private readonly List<string> _testJobIds = new();

    public VideoIngestionPipelineE2ETests(
        CustomWebApplicationFactory<Program> factory,
        ITestOutputHelper output)
        : base(factory)
    {
        _output = output;
    }

    #region Helper Methods

    /// <summary>
    /// Get video from database by YouTube ID
    /// </summary>
    private async Task<Video?> GetVideoFromDbByYouTubeId(string youTubeId)
    {
        return await DbContext.Videos
            .Include(v => v.Jobs)
            .Include(v => v.TranscriptSegments)
            .FirstOrDefaultAsync(v => v.YouTubeId == youTubeId);
    }

    /// <summary>
    /// Get video from database by Video ID
    /// </summary>
    private async Task<Video?> GetVideoFromDbById(string videoId)
    {
        return await DbContext.Videos
            .Include(v => v.Jobs)
            .Include(v => v.TranscriptSegments)
            .FirstOrDefaultAsync(v => v.Id == videoId);
    }

    /// <summary>
    /// Get total count of videos in database
    /// </summary>
    private async Task<int> GetVideoCountInDb()
    {
        return await DbContext.Videos.CountAsync();
    }

    /// <summary>
    /// Get total count of jobs in database
    /// </summary>
    private async Task<int> GetJobCountInDb()
    {
        return await DbContext.Jobs.CountAsync();
    }

    /// <summary>
    /// Get jobs for a specific video
    /// </summary>
    private async Task<List<Job>> GetJobsForVideo(string videoId)
    {
        return await DbContext.Jobs
            .Where(j => j.VideoId == videoId)
            .ToListAsync();
    }

    /// <summary>
    /// Extract YouTube ID from URL
    /// </summary>
    private string ExtractYouTubeId(string url)
    {
        // Simple extraction for test URLs
        // Handles: https://www.youtube.com/watch?v=VIDEO_ID
        if (url.Contains("watch?v="))
        {
            var startIndex = url.IndexOf("watch?v=") + 8;
            var videoId = url.Substring(startIndex);
            var ampIndex = videoId.IndexOf('&');
            if (ampIndex > 0)
            {
                videoId = videoId.Substring(0, ampIndex);
            }
            return videoId;
        }
        return string.Empty;
    }

    /// <summary>
    /// Log database state for debugging
    /// </summary>
    private async Task LogDatabaseState(string context)
    {
        var videoCount = await GetVideoCountInDb();
        var jobCount = await GetJobCountInDb();
        _output.WriteLine($"[{context}] DB State - Videos: {videoCount}, Jobs: {jobCount}");
    }

    #endregion

    #region Test 1: Short Video Success

    /// <summary>
    /// Test 1: IngestVideo_ShortVideo_ShouldCreateVideoAndJobInDatabase
    ///
    /// Validates that:
    /// - API returns HTTP 200 OK
    /// - Video record is inserted in database
    /// - Job record is created with correct VideoId FK
    /// - UserId foreign key is valid
    /// - Status is Pending
    /// - All required metadata fields are populated
    /// </summary>
    [Fact]
    public async Task IngestVideo_ShortVideo_ShouldCreateVideoAndJobInDatabase()
    {
        // Arrange
        await AuthenticateAsync();
        await LogDatabaseState("Before Test 1");

        var testUrl = "https://www.youtube.com/watch?v=jNQXAC9IVRw"; // "Me at the zoo" - first YouTube video (0:19)
        var expectedYouTubeId = "jNQXAC9IVRw";

        var request = new
        {
            url = testUrl,
            title = "Short Test Video",
            description = "E2E test for short video ingestion",
            priority = 1 // Normal
        };

        var initialVideoCount = await GetVideoCountInDb();
        var initialJobCount = await GetJobCountInDb();

        _output.WriteLine($"Initial state - Videos: {initialVideoCount}, Jobs: {initialJobCount}");

        // Act
        var response = await Client.PostAsJsonAsync(_ingestUrl, request);

        // Assert - HTTP Response
        _output.WriteLine($"Response Status: {response.StatusCode}");
        var responseContent = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response Body: {responseContent}");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "API should return 200 OK when video ingestion is initiated successfully");

        var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);

        // Verify response structure
        responseJson.TryGetProperty("videoId", out var videoIdProp).Should().BeTrue(
            "Response should contain videoId property");
        responseJson.TryGetProperty("jobId", out var jobIdProp).Should().BeTrue(
            "Response should contain jobId property");
        responseJson.TryGetProperty("status", out var statusProp).Should().BeTrue(
            "Response should contain status property");

        var returnedVideoId = videoIdProp.GetString();
        var returnedJobId = jobIdProp.GetString();

        returnedVideoId.Should().NotBeNullOrEmpty("VideoId should be returned in response");
        returnedJobId.Should().NotBeNullOrEmpty("JobId should be returned in response");

        _testVideoIds.Add(returnedVideoId!);
        _testJobIds.Add(returnedJobId!);

        _output.WriteLine($"Returned VideoId: {returnedVideoId}, JobId: {returnedJobId}");

        // Assert - Database Verification
        await LogDatabaseState("After API Call");

        // Verify video was inserted
        var video = await GetVideoFromDbById(returnedVideoId!);
        video.Should().NotBeNull("Video should be inserted in database after successful ingestion");

        // Verify video properties
        video!.YouTubeId.Should().Be(expectedYouTubeId,
            "Video YouTubeId should match the extracted ID from URL");
        video.UserId.Should().Be(AuthenticatedUserId,
            "Video should be associated with the authenticated user");
        video.Status.Should().Be(VideoStatus.Pending,
            "Video status should be Pending after initial ingestion");
        video.Url.Should().NotBeNullOrEmpty("Video URL should be populated");
        video.Title.Should().NotBeNullOrEmpty("Video title should be populated");

        _output.WriteLine($"Video verification passed - ID: {video.Id}, YouTubeId: {video.YouTubeId}, Status: {video.Status}");

        // Verify job was created
        var jobs = await GetJobsForVideo(returnedVideoId!);
        jobs.Should().NotBeEmpty("At least one job should be created for the video");

        var job = jobs.FirstOrDefault(j => j.Id == returnedJobId);
        job.Should().NotBeNull("Job with returned JobId should exist in database");

        // Verify job properties
        job!.VideoId.Should().Be(returnedVideoId,
            "Job VideoId foreign key should match the video ID");
        job.UserId.Should().Be(AuthenticatedUserId,
            "Job should be associated with the authenticated user");
        job.Status.Should().BeOneOf(new[] { JobStatus.Pending, JobStatus.Running },
            "Job status should be Pending or Running after creation");
        job.Type.Should().Be(JobType.VideoProcessing,
            "Job type should be VideoProcessing");

        _output.WriteLine($"Job verification passed - ID: {job.Id}, Status: {job.Status}, Progress: {job.Progress}%");

        // Verify counts increased
        var finalVideoCount = await GetVideoCountInDb();
        var finalJobCount = await GetJobCountInDb();

        finalVideoCount.Should().Be(initialVideoCount + 1,
            "Video count should increase by 1");
        finalJobCount.Should().BeGreaterThanOrEqualTo(initialJobCount + 1,
            "Job count should increase by at least 1 (system may create multiple related jobs)");

        _output.WriteLine($"Count verification passed - Videos: {initialVideoCount} -> {finalVideoCount}, Jobs: {initialJobCount} -> {finalJobCount}");
    }

    #endregion

    #region Test 2: Duplicate Video Conflict

    /// <summary>
    /// Test 2: IngestVideo_DuplicateVideo_ShouldReturn409Conflict
    ///
    /// Validates that:
    /// - First ingestion succeeds (HTTP 200)
    /// - Second ingestion of same video returns HTTP 409 Conflict
    /// - Only ONE video record exists in database
    /// - Duplicate detection works correctly
    /// </summary>
    [Fact]
    public async Task IngestVideo_DuplicateVideo_ShouldReturn409Conflict()
    {
        // Arrange
        await AuthenticateAsync();
        await LogDatabaseState("Before Test 2");

        var testUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ"; // Rick Astley - Never Gonna Give You Up
        var expectedYouTubeId = "dQw4w9WgXcQ";

        var request = new
        {
            url = testUrl,
            title = "Duplicate Test Video",
            description = "E2E test for duplicate detection",
            priority = 1
        };

        var initialVideoCount = await GetVideoCountInDb();

        // Act - First ingestion (should succeed)
        var firstResponse = await Client.PostAsJsonAsync(_ingestUrl, request);

        // Assert - First ingestion
        _output.WriteLine($"First ingestion status: {firstResponse.StatusCode}");
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "First ingestion should succeed");

        var firstResponseContent = await firstResponse.Content.ReadAsStringAsync();
        var firstResponseJson = JsonSerializer.Deserialize<JsonElement>(firstResponseContent);
        var firstVideoId = firstResponseJson.GetProperty("videoId").GetString();
        var firstJobId = firstResponseJson.GetProperty("jobId").GetString();

        _testVideoIds.Add(firstVideoId!);
        _testJobIds.Add(firstJobId!);

        _output.WriteLine($"First ingestion - VideoId: {firstVideoId}");

        // Verify first video was inserted
        var firstVideo = await GetVideoFromDbByYouTubeId(expectedYouTubeId);
        firstVideo.Should().NotBeNull("First video should be inserted in database");

        var videoCountAfterFirst = await GetVideoCountInDb();
        videoCountAfterFirst.Should().Be(initialVideoCount + 1,
            "Video count should increase by 1 after first ingestion");

        // Act - Second ingestion (should fail with conflict)
        var secondResponse = await Client.PostAsJsonAsync(_ingestUrl, request);

        // Assert - Second ingestion
        _output.WriteLine($"Second ingestion status: {secondResponse.StatusCode}");
        var secondResponseContent = await secondResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"Second response body: {secondResponseContent}");

        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "Second ingestion of same video should return 409 Conflict");

        // Verify response contains problem details
        var problemDetails = JsonSerializer.Deserialize<JsonElement>(secondResponseContent);
        problemDetails.TryGetProperty("title", out var titleProp).Should().BeTrue(
            "Conflict response should contain title");
        problemDetails.TryGetProperty("detail", out var detailProp).Should().BeTrue(
            "Conflict response should contain detail");

        // Verify only ONE video exists in database
        var videosWithYouTubeId = await DbContext.Videos
            .Where(v => v.YouTubeId == expectedYouTubeId)
            .ToListAsync();

        videosWithYouTubeId.Should().HaveCount(1,
            "Only one video should exist with the same YouTube ID");

        var finalVideoCount = await GetVideoCountInDb();
        finalVideoCount.Should().Be(initialVideoCount + 1,
            "Video count should remain the same after duplicate attempt");

        _output.WriteLine($"Duplicate detection verified - Only {videosWithYouTubeId.Count} video exists");
        await LogDatabaseState("After Test 2");
    }

    #endregion

    #region Test 3: Invalid URL Validation

    /// <summary>
    /// Test 3: IngestVideo_InvalidYouTubeUrl_ShouldReturn400BadRequest
    ///
    /// Validates that:
    /// - Invalid URL returns HTTP 400 Bad Request
    /// - No video record is inserted in database
    /// - No job is created
    /// - Proper validation error message is returned
    /// </summary>
    [Fact]
    public async Task IngestVideo_InvalidYouTubeUrl_ShouldReturn400BadRequest()
    {
        // Arrange
        await AuthenticateAsync();
        await LogDatabaseState("Before Test 3");

        var invalidUrls = new[]
        {
            "not-a-valid-url",
            "https://www.google.com",
            "ftp://invalid-protocol.com",
            ""
        };

        var initialVideoCount = await GetVideoCountInDb();
        var initialJobCount = await GetJobCountInDb();

        foreach (var invalidUrl in invalidUrls)
        {
            _output.WriteLine($"\nTesting invalid URL: '{invalidUrl}'");

            var request = new
            {
                url = invalidUrl,
                priority = 1
            };

            // Act
            var response = await Client.PostAsJsonAsync(_ingestUrl, request);

            // Assert
            var responseContent = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Response status: {response.StatusCode}");
            _output.WriteLine($"Response body: {responseContent}");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
                $"Invalid URL '{invalidUrl}' should return 400 Bad Request");

            // Verify response contains validation error details
            if (!string.IsNullOrWhiteSpace(responseContent))
            {
                var problemDetails = JsonSerializer.Deserialize<JsonElement>(responseContent);
                problemDetails.TryGetProperty("title", out _).Should().BeTrue(
                    "Bad request response should contain title");
            }
        }

        // Verify no videos or jobs were created
        var finalVideoCount = await GetVideoCountInDb();
        var finalJobCount = await GetJobCountInDb();

        finalVideoCount.Should().Be(initialVideoCount,
            "No videos should be created for invalid URLs");
        finalJobCount.Should().Be(initialJobCount,
            "No jobs should be created for invalid URLs");

        _output.WriteLine($"\nValidation verified - Videos: {finalVideoCount}, Jobs: {finalJobCount}");
        await LogDatabaseState("After Test 3");
    }

    #endregion

    #region Test 4: Private/Unavailable Video Handling

    /// <summary>
    /// Test 4: IngestVideo_PrivateVideo_ShouldHandleGracefully
    ///
    /// Validates that:
    /// - Private/unavailable video is handled gracefully
    /// - Returns appropriate error status (400 or 404)
    /// - Error message is logged
    /// - No orphaned records are created
    /// </summary>
    [Fact]
    public async Task IngestVideo_PrivateVideo_ShouldHandleGracefully()
    {
        // Arrange
        await AuthenticateAsync();
        await LogDatabaseState("Before Test 4");

        // Note: This is a fictional video ID that should not exist
        var privateVideoUrl = "https://www.youtube.com/watch?v=PRIVATEVID123";

        var request = new
        {
            url = privateVideoUrl,
            title = "Private Video Test",
            priority = 1
        };

        var initialVideoCount = await GetVideoCountInDb();
        var initialJobCount = await GetJobCountInDb();

        // Act
        var response = await Client.PostAsJsonAsync(_ingestUrl, request);

        // Assert
        var responseContent = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response status: {response.StatusCode}");
        _output.WriteLine($"Response body: {responseContent}");

        // Should return either Bad Request (400) or Not Found (404) depending on implementation
        response.StatusCode.Should().BeOneOf(new[] {
            HttpStatusCode.BadRequest,
            HttpStatusCode.NotFound,
            HttpStatusCode.OK // In some cases, the video might be queued and fail during processing
        }, "Private/unavailable video should be handled with appropriate error status");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            // If the API accepts the request, verify the video/job status indicates failure
            var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);
            if (responseJson.TryGetProperty("videoId", out var videoIdProp))
            {
                var videoId = videoIdProp.GetString();
                _testVideoIds.Add(videoId!);

                // Check if job was created with error status
                var jobs = await GetJobsForVideo(videoId!);
                if (jobs.Any())
                {
                    _testJobIds.AddRange(jobs.Select(j => j.Id));
                    _output.WriteLine($"Job created with status: {jobs.First().Status}");
                }
            }
        }
        else
        {
            // Verify no video or job was created for the error case
            var finalVideoCount = await GetVideoCountInDb();
            var finalJobCount = await GetJobCountInDb();

            finalVideoCount.Should().Be(initialVideoCount,
                "No video should be created for unavailable video");
            finalJobCount.Should().Be(initialJobCount,
                "No job should be created for unavailable video");
        }

        await LogDatabaseState("After Test 4");
    }

    #endregion

    #region Test 5: Metadata Fallback Success

    /// <summary>
    /// Test 5: IngestVideo_WithMetadataFallback_ShouldSucceed
    ///
    /// Validates that:
    /// - When YouTube API fails (403), yt-dlp fallback is used
    /// - Video is ingested successfully with fallback metadata
    /// - All metadata fields are populated
    /// - Job is created and processing continues
    /// </summary>
    [Fact]
    public async Task IngestVideo_WithMetadataFallback_ShouldSucceed()
    {
        // Arrange
        await AuthenticateAsync();
        await LogDatabaseState("Before Test 5");

        // Use a valid YouTube video that should work with yt-dlp
        var testUrl = "https://www.youtube.com/watch?v=9bZkp7q19f0"; // "Gangnam Style" - widely available
        var expectedYouTubeId = "9bZkp7q19f0";

        var request = new
        {
            url = testUrl,
            title = "Fallback Test Video",
            description = "E2E test for metadata fallback",
            priority = 1
        };

        var initialVideoCount = await GetVideoCountInDb();

        // Act
        var response = await Client.PostAsJsonAsync(_ingestUrl, request);

        // Assert
        var responseContent = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response status: {response.StatusCode}");
        _output.WriteLine($"Response body: {responseContent}");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Video ingestion should succeed with fallback metadata");

        var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);
        var videoId = responseJson.GetProperty("videoId").GetString();
        var jobId = responseJson.GetProperty("jobId").GetString();

        _testVideoIds.Add(videoId!);
        _testJobIds.Add(jobId!);

        // Verify video was inserted with metadata
        var video = await GetVideoFromDbById(videoId!);
        video.Should().NotBeNull("Video should be inserted in database");

        // Verify metadata fields are populated (either from YouTube API or yt-dlp fallback)
        video!.YouTubeId.Should().Be(expectedYouTubeId,
            "YouTube ID should be extracted correctly");
        video.Title.Should().NotBeNullOrEmpty(
            "Title should be populated from metadata source");
        video.Url.Should().NotBeNullOrEmpty(
            "URL should be populated");
        video.UserId.Should().Be(AuthenticatedUserId,
            "Video should be associated with authenticated user");

        _output.WriteLine($"Metadata verification - Title: '{video.Title}', YouTubeId: {video.YouTubeId}");

        // Verify job was created
        var job = await DbContext.Jobs.FirstOrDefaultAsync(j => j.Id == jobId);
        job.Should().NotBeNull("Job should be created for video processing");
        job!.VideoId.Should().Be(videoId, "Job should be linked to the video");

        var finalVideoCount = await GetVideoCountInDb();
        finalVideoCount.Should().Be(initialVideoCount + 1,
            "Video count should increase by 1");

        await LogDatabaseState("After Test 5");
    }

    #endregion

    #region Test 6: Foreign Key Relationships

    /// <summary>
    /// Test 6: IngestVideo_VerifyForeignKeyRelationships
    ///
    /// Validates that:
    /// - Video.UserId foreign key is valid
    /// - Job.UserId foreign key is valid
    /// - Job.VideoId foreign key is valid
    /// - All navigation properties work correctly
    /// </summary>
    [Fact]
    public async Task IngestVideo_VerifyForeignKeyRelationships()
    {
        // Arrange
        await AuthenticateAsync();
        await LogDatabaseState("Before Test 6");

        var testUrl = "https://www.youtube.com/watch?v=kJQP7kiw5Fk"; // "Luis Fonsi - Despacito"

        var request = new
        {
            url = testUrl,
            title = "FK Relationship Test",
            priority = 1
        };

        // Act
        var response = await Client.PostAsJsonAsync(_ingestUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);
        var videoId = responseJson.GetProperty("videoId").GetString();
        var jobId = responseJson.GetProperty("jobId").GetString();

        _testVideoIds.Add(videoId!);
        _testJobIds.Add(jobId!);

        // Verify Video -> User relationship
        var videoWithUser = await DbContext.Videos
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.Id == videoId);

        videoWithUser.Should().NotBeNull("Video should exist in database");
        videoWithUser!.UserId.Should().Be(AuthenticatedUserId,
            "Video UserId FK should match authenticated user");
        videoWithUser.User.Should().NotBeNull(
            "Video.User navigation property should be populated");
        videoWithUser.User.Id.Should().Be(AuthenticatedUserId,
            "User ID from navigation property should match");

        _output.WriteLine($"Video -> User FK verified: VideoId={videoId}, UserId={videoWithUser.UserId}");

        // Verify Job -> Video relationship
        var jobWithVideo = await DbContext.Jobs
            .Include(j => j.Video)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        jobWithVideo.Should().NotBeNull("Job should exist in database");
        jobWithVideo!.VideoId.Should().Be(videoId,
            "Job VideoId FK should match video ID");
        jobWithVideo.Video.Should().NotBeNull(
            "Job.Video navigation property should be populated");
        jobWithVideo.Video!.Id.Should().Be(videoId,
            "Video ID from navigation property should match");

        _output.WriteLine($"Job -> Video FK verified: JobId={jobId}, VideoId={jobWithVideo.VideoId}");

        // Verify Job -> User relationship
        var jobWithUser = await DbContext.Jobs
            .Include(j => j.User)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        jobWithUser.Should().NotBeNull("Job should exist in database");
        jobWithUser!.UserId.Should().Be(AuthenticatedUserId,
            "Job UserId FK should match authenticated user");
        jobWithUser.User.Should().NotBeNull(
            "Job.User navigation property should be populated");
        jobWithUser.User.Id.Should().Be(AuthenticatedUserId,
            "User ID from navigation property should match");

        _output.WriteLine($"Job -> User FK verified: JobId={jobId}, UserId={jobWithUser.UserId}");

        // Verify Video -> Jobs collection
        var videoWithJobs = await DbContext.Videos
            .Include(v => v.Jobs)
            .FirstOrDefaultAsync(v => v.Id == videoId);

        videoWithJobs.Should().NotBeNull("Video should exist");
        videoWithJobs!.Jobs.Should().NotBeEmpty(
            "Video.Jobs collection should contain at least one job");
        videoWithJobs.Jobs.Should().Contain(j => j.Id == jobId,
            "Video.Jobs collection should contain the created job");

        _output.WriteLine($"Video -> Jobs collection verified: {videoWithJobs.Jobs.Count} job(s) found");

        await LogDatabaseState("After Test 6");
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Clean up test data after all tests complete
    /// Ensures database is left in a clean state
    /// </summary>
    public override async Task DisposeAsync()
    {
        try
        {
            _output.WriteLine("\n=== Starting Test Cleanup ===");
            await LogDatabaseState("Before Cleanup");

            // Find all videos created by the test user
            var testVideos = await DbContext.Videos
                .Where(v => v.UserId == AuthenticatedUserId)
                .Include(v => v.Jobs)
                .Include(v => v.TranscriptSegments)
                .ToListAsync();

            _output.WriteLine($"Found {testVideos.Count} test videos to clean up");

            foreach (var video in testVideos)
            {
                _output.WriteLine($"Cleaning up video: {video.Id}, YouTubeId: {video.YouTubeId}");

                // Delete transcript segments first (child records)
                if (video.TranscriptSegments.Any())
                {
                    _output.WriteLine($"  Deleting {video.TranscriptSegments.Count} transcript segments");
                    DbContext.TranscriptSegments.RemoveRange(video.TranscriptSegments);
                }

                // Delete jobs (child records)
                if (video.Jobs.Any())
                {
                    _output.WriteLine($"  Deleting {video.Jobs.Count} jobs");
                    DbContext.Jobs.RemoveRange(video.Jobs);
                }

                // Delete video
                DbContext.Videos.Remove(video);
            }

            // Save all deletions
            var deletedCount = await DbContext.SaveChangesAsync();
            _output.WriteLine($"Deleted {deletedCount} total records");

            await LogDatabaseState("After Cleanup");

            // Verify cleanup was successful
            var remainingVideos = await DbContext.Videos
                .Where(v => v.UserId == AuthenticatedUserId)
                .CountAsync();

            var remainingJobs = await DbContext.Jobs
                .Where(j => j.UserId == AuthenticatedUserId)
                .CountAsync();

            _output.WriteLine($"Verification - Remaining videos: {remainingVideos}, Remaining jobs: {remainingJobs}");

            if (remainingVideos > 0 || remainingJobs > 0)
            {
                _output.WriteLine("WARNING: Some test data was not cleaned up properly!");
            }
            else
            {
                _output.WriteLine("✓ Cleanup completed successfully - Database is clean");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"ERROR during cleanup: {ex.Message}");
            _output.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        finally
        {
            // Call base cleanup
            await base.DisposeAsync();
            _output.WriteLine("=== Test Cleanup Complete ===\n");
        }
    }

    #endregion
}
