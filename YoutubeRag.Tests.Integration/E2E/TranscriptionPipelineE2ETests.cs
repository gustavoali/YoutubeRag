using System.Diagnostics;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;
using YoutubeRag.Application.Configuration;
using YoutubeRag.Application.DTOs.Transcription;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Application.Interfaces.Services;
using YoutubeRag.Application.Services;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;
using YoutubeRag.Tests.Integration.Helpers;
using YoutubeRag.Tests.Integration.Infrastructure;

namespace YoutubeRag.Tests.Integration.E2E;

/// <summary>
/// End-to-End tests for the complete Transcription Pipeline (Epic 2)
/// Tests the full flow: Job creation → Audio extraction → Whisper transcription → Segment storage
/// Validates: Bulk insert, auto-segmentation, error handling, and state transitions
/// </summary>
public class TranscriptionPipelineE2ETests : IntegrationTestBase
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IAudioExtractionService> _mockAudioExtractionService;
    private readonly Mock<IVideoDownloadService> _mockVideoDownloadService;
    private readonly Mock<ITranscriptionService> _mockTranscriptionService;
    private readonly Mock<IAppConfiguration> _mockAppConfiguration;
    private readonly Mock<IProgressNotificationService> _mockProgressNotificationService;

    public TranscriptionPipelineE2ETests(
        CustomWebApplicationFactory<Program> factory,
        ITestOutputHelper output) : base(factory)
    {
        _output = output;
        _mockAudioExtractionService = new Mock<IAudioExtractionService>();
        _mockVideoDownloadService = new Mock<IVideoDownloadService>();
        _mockTranscriptionService = new Mock<ITranscriptionService>();
        _mockAppConfiguration = new Mock<IAppConfiguration>();
        _mockProgressNotificationService = new Mock<IProgressNotificationService>();

        // Default configuration
        _mockAppConfiguration.Setup(x => x.AutoGenerateEmbeddings).Returns(false);

        // Default mock for video download service
        _mockVideoDownloadService
            .Setup(x => x.DownloadVideoAsync(It.IsAny<string>(), It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string youtubeId, IProgress<double> progress, CancellationToken ct) =>
                $"C:\\temp\\{youtubeId}_video.mp4");

        // Default mock for Whisper audio extraction
        _mockAudioExtractionService
            .Setup(x => x.ExtractWhisperAudioFromVideoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string videoPath, string videoId, CancellationToken ct) =>
                $"C:\\temp\\{videoId}_whisper.wav");
    }

    private TranscriptionJobProcessor CreateProcessor()
    {
        return new TranscriptionJobProcessor(
            Scope.ServiceProvider.GetRequiredService<IVideoRepository>(),
            Scope.ServiceProvider.GetRequiredService<IJobRepository>(),
            Scope.ServiceProvider.GetRequiredService<ITranscriptSegmentRepository>(),
            Scope.ServiceProvider.GetRequiredService<IDeadLetterJobRepository>(),
            _mockAudioExtractionService.Object,
            _mockVideoDownloadService.Object,
            _mockTranscriptionService.Object,
            Scope.ServiceProvider.GetRequiredService<ISegmentationService>(),
            Scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
            _mockAppConfiguration.Object,
            Scope.ServiceProvider.GetRequiredService<IBackgroundJobService>(),
            _mockProgressNotificationService.Object,
            Scope.ServiceProvider.GetRequiredService<ILogger<TranscriptionJobProcessor>>()
        );
    }

    #region Test 1: Complete Pipeline - Video Corto (<5 min)

    /// <summary>
    /// Test 1: Complete End-to-End Pipeline for Short Video
    ///
    /// Validates:
    /// - Job created with status Completed
    /// - Video status = Completed, TranscriptionStatus = Completed
    /// - Segments saved to DB (>0)
    /// - Segments with sequential SegmentIndex (0, 1, 2, ...)
    /// - Timestamps valid and increasing
    /// - Text not empty in each segment
    /// - Bulk insert used (verify via logs or count)
    /// </summary>
    [Fact]
    public async Task CompleteTranscriptionPipeline_ShortVideo_ShouldProcessSuccessfully()
    {
        // Arrange
        _output.WriteLine("=== Test 1: Complete Pipeline - Short Video ===");
        await AuthenticateAsync();

        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        video.Status = VideoStatus.Pending;
        video.TranscriptionStatus = TranscriptionStatus.NotStarted;
        video.Duration = TimeSpan.FromMinutes(3); // Short video

        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        _output.WriteLine($"Created test video: {video.Id}, YouTubeId: {video.YouTubeId}");

        var processor = CreateProcessor();

        // Setup mocks for successful short video processing
        _mockTranscriptionService
            .Setup(x => x.IsWhisperAvailableAsync())
            .ReturnsAsync(true);

        _mockAudioExtractionService
            .Setup(x => x.ExtractAudioFromYouTubeAsync(video.YouTubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("C:\\temp\\audio_short.mp3");

        _mockAudioExtractionService
            .Setup(x => x.GetAudioInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AudioInfo
            {
                Duration = TimeSpan.FromMinutes(3),
                FileSizeBytes = 2500000
            });

        // Create realistic segments for 3-minute video
        var mockSegments = new List<TranscriptSegmentDto>
        {
            new() { StartTime = 0, EndTime = 5.2, Text = "Welcome to this short tutorial on testing.", Confidence = 0.95 },
            new() { StartTime = 5.2, EndTime = 12.8, Text = "Today we'll cover end-to-end testing in .NET applications.", Confidence = 0.92 },
            new() { StartTime = 12.8, EndTime = 25.5, Text = "First, let's understand what integration tests are and why they matter.", Confidence = 0.94 },
            new() { StartTime = 25.5, EndTime = 40.0, Text = "Integration tests verify that multiple components work together correctly.", Confidence = 0.96 },
            new() { StartTime = 40.0, EndTime = 58.3, Text = "Unlike unit tests, integration tests interact with real dependencies like databases.", Confidence = 0.93 },
            new() { StartTime = 58.3, EndTime = 75.0, Text = "Let's look at an example of how to write these tests.", Confidence = 0.95 },
            new() { StartTime = 75.0, EndTime = 95.5, Text = "Here we create a test database, seed data, and verify the complete workflow.", Confidence = 0.91 },
            new() { StartTime = 95.5, EndTime = 120.0, Text = "Remember to clean up your test data after each test to ensure isolation.", Confidence = 0.94 },
            new() { StartTime = 120.0, EndTime = 145.8, Text = "This approach helps catch bugs that unit tests might miss.", Confidence = 0.96 },
            new() { StartTime = 145.8, EndTime = 180.0, Text = "That's all for today. Thanks for watching and happy testing!", Confidence = 0.97 }
        };

        _mockTranscriptionService
            .Setup(x => x.TranscribeAudioAsync(It.IsAny<TranscriptionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TranscriptionResultDto
            {
                VideoId = video.Id,
                Duration = TimeSpan.FromMinutes(3),
                Language = "en",
                Segments = mockSegments,
                Confidence = 0.94,
                ModelUsed = "tiny"
            });

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await processor.ProcessTranscriptionJobAsync(video.Id);
        stopwatch.Stop();

        _output.WriteLine($"Pipeline execution time: {stopwatch.ElapsedMilliseconds}ms");

        // Assert - Job Status
        result.Should().BeTrue("Pipeline should complete successfully");

        var job = await DbContext.Jobs
            .FirstOrDefaultAsync(j => j.VideoId == video.Id && j.Type == JobType.Transcription);

        job.Should().NotBeNull("Transcription job should be created");
        job!.Status.Should().Be(JobStatus.Completed, "Job status should be Completed");
        job.Progress.Should().Be(100, "Progress should be 100%");
        job.StartedAt.Should().NotBeNull("StartedAt should be set");
        job.CompletedAt.Should().NotBeNull("CompletedAt should be set");

        _output.WriteLine($"Job verification: Status={job.Status}, Progress={job.Progress}%");

        // Assert - Video Status
        var updatedVideo = await DbContext.Videos.FindAsync(video.Id);
        updatedVideo.Should().NotBeNull();
        updatedVideo!.ProcessingStatus.Should().Be(VideoStatus.Completed, "Video status should be Completed");
        updatedVideo.TranscriptionStatus.Should().Be(TranscriptionStatus.Completed, "Transcription status should be Completed");
        updatedVideo.TranscribedAt.Should().NotBeNull("TranscribedAt should be set");
        updatedVideo.Language.Should().Be("en", "Language should be detected");

        _output.WriteLine($"Video verification: Status={updatedVideo.ProcessingStatus}, TranscriptionStatus={updatedVideo.TranscriptionStatus}");

        // Assert - Segments Saved
        var segments = await DbContext.TranscriptSegments
            .Where(s => s.VideoId == video.Id)
            .OrderBy(s => s.SegmentIndex)
            .ToListAsync();

        segments.Should().NotBeEmpty("Segments should be saved to database");
        segments.Count.Should().Be(mockSegments.Count, "All segments should be saved");

        _output.WriteLine($"Segments saved: {segments.Count}");

        // Assert - Sequential SegmentIndex
        for (int i = 0; i < segments.Count; i++)
        {
            segments[i].SegmentIndex.Should().Be(i, $"Segment {i} should have SegmentIndex={i}");
        }

        _output.WriteLine("✓ Sequential SegmentIndex verified (0, 1, 2, ...)");

        // Assert - Timestamps Valid and Increasing
        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];

            // StartTime and EndTime should be valid
            segment.StartTime.Should().BeGreaterThanOrEqualTo(0, $"Segment {i} StartTime should be >= 0");
            segment.EndTime.Should().BeGreaterThan(segment.StartTime, $"Segment {i} EndTime should be > StartTime");

            // Timestamps should be increasing
            if (i > 0)
            {
                segment.StartTime.Should().BeGreaterThanOrEqualTo(segments[i - 1].StartTime,
                    $"Segment {i} StartTime should be >= previous segment's StartTime");
            }
        }

        _output.WriteLine("✓ Timestamps valid and increasing");

        // Assert - Text Not Empty
        foreach (var segment in segments)
        {
            segment.Text.Should().NotBeNullOrWhiteSpace($"Segment {segment.SegmentIndex} text should not be empty");
        }

        _output.WriteLine("✓ All segments have non-empty text");

        // Assert - Bulk Insert Verification (indirect - check all segments inserted at once)
        var createdAtTimes = segments.Select(s => s.CreatedAt).Distinct().ToList();
        createdAtTimes.Should().HaveCount(1, "All segments should be created at the same time (bulk insert)");

        _output.WriteLine($"✓ Bulk insert verified - All segments created at: {createdAtTimes.First()}");
        _output.WriteLine("=== Test 1 PASSED ===\n");
    }

    #endregion

    #region Test 2: Long Segments Auto-Split

    /// <summary>
    /// Test 2: Long Segments Auto-Split
    ///
    /// Validates:
    /// - Mock Whisper returns segments >500 chars
    /// - SegmentationService divides segments
    /// - Re-indexation correct
    /// - Timestamps distributed proportionally
    /// </summary>
    [Fact]
    public async Task TranscriptionPipeline_LongSegments_ShouldAutoSplitAndReindex()
    {
        // Arrange
        _output.WriteLine("=== Test 2: Long Segments Auto-Split ===");
        await AuthenticateAsync();

        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        video.Status = VideoStatus.Pending;
        video.TranscriptionStatus = TranscriptionStatus.NotStarted;

        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        _output.WriteLine($"Created test video: {video.Id}");

        var processor = CreateProcessor();

        // Setup mocks
        _mockTranscriptionService.Setup(x => x.IsWhisperAvailableAsync()).ReturnsAsync(true);
        _mockAudioExtractionService
            .Setup(x => x.ExtractAudioFromYouTubeAsync(video.YouTubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("C:\\temp\\audio_long_segments.mp3");
        _mockAudioExtractionService
            .Setup(x => x.GetAudioInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AudioInfo { Duration = TimeSpan.FromMinutes(10), FileSizeBytes = 8000000 });

        // Create segments with VERY LONG TEXT (>500 chars) to trigger auto-split
        var longText1 = new string('A', 750) + " This is a very long transcript segment that exceeds the maximum allowed length of 500 characters. " +
                        "The system should automatically split this into multiple smaller segments while preserving the timestamps and metadata. " +
                        "Each sub-segment should be properly indexed and the timestamps should be distributed proportionally across the split segments.";

        var longText2 = "In this comprehensive tutorial, we will explore advanced topics in software engineering including " +
                        "microservices architecture, event-driven design patterns, domain-driven design principles, continuous integration " +
                        "and deployment strategies, containerization with Docker and Kubernetes, cloud-native application development, " +
                        "observability and monitoring best practices, security considerations in distributed systems, performance optimization " +
                        "techniques, and scalability patterns for high-traffic applications. We'll also cover testing strategies including unit tests, " +
                        "integration tests, end-to-end tests, and chaos engineering. This segment is intentionally very long to test the auto-splitting functionality.";

        var mockSegments = new List<TranscriptSegmentDto>
        {
            new() { StartTime = 0, EndTime = 30, Text = "Short segment that won't be split.", Confidence = 0.95 },
            new() { StartTime = 30, EndTime = 90, Text = longText1, Confidence = 0.92 }, // Will be split
            new() { StartTime = 90, EndTime = 150, Text = longText2, Confidence = 0.94 }, // Will be split
            new() { StartTime = 150, EndTime = 180, Text = "Another short segment.", Confidence = 0.96 }
        };

        _mockTranscriptionService
            .Setup(x => x.TranscribeAudioAsync(It.IsAny<TranscriptionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TranscriptionResultDto
            {
                VideoId = video.Id,
                Duration = TimeSpan.FromMinutes(10),
                Language = "en",
                Segments = mockSegments
            });

        // Act
        var result = await processor.ProcessTranscriptionJobAsync(video.Id);

        // Assert
        result.Should().BeTrue();

        var segments = await DbContext.TranscriptSegments
            .Where(s => s.VideoId == video.Id)
            .OrderBy(s => s.SegmentIndex)
            .ToListAsync();

        _output.WriteLine($"Original segments: {mockSegments.Count}, Final segments after split: {segments.Count}");

        // Should have MORE segments than original due to splitting
        segments.Count.Should().BeGreaterThan(mockSegments.Count,
            "Long segments should be split into multiple segments");

        // Verify sequential re-indexing
        for (int i = 0; i < segments.Count; i++)
        {
            segments[i].SegmentIndex.Should().Be(i, $"Segment {i} should have correct SegmentIndex after re-indexing");
        }

        _output.WriteLine("✓ Sequential re-indexation verified");

        // Verify each segment is <= 500 chars
        foreach (var segment in segments)
        {
            segment.Text.Length.Should().BeLessThanOrEqualTo(500,
                $"Segment {segment.SegmentIndex} should not exceed 500 characters");
        }

        _output.WriteLine("✓ All segments <= 500 characters");

        // Verify timestamps don't overlap
        for (int i = 1; i < segments.Count; i++)
        {
            segments[i].StartTime.Should().BeGreaterThanOrEqualTo(segments[i - 1].EndTime,
                $"Segment {i} should not overlap with previous segment");
        }

        _output.WriteLine("✓ No timestamp overlaps detected");

        // Verify timestamps are proportionally distributed
        // Check that sub-segments from the same original segment have similar durations
        var segment1SubSegments = segments.Where(s => s.StartTime >= 30 && s.EndTime <= 90).ToList();
        if (segment1SubSegments.Count > 1)
        {
            var totalDuration = 90 - 30; // Original segment duration
            var sumSubDurations = segment1SubSegments.Sum(s => s.EndTime - s.StartTime);
            Math.Abs(sumSubDurations - totalDuration).Should().BeLessThan(1.0,
                "Sum of sub-segment durations should approximately equal original duration");
        }

        _output.WriteLine($"✓ Timestamps distributed proportionally across {segment1SubSegments.Count} sub-segments");
        _output.WriteLine("=== Test 2 PASSED ===\n");
    }

    #endregion

    #region Test 3: SegmentationService Integration

    /// <summary>
    /// Test 3: SegmentationService Integration
    ///
    /// Validates:
    /// - Segment of 1200 characters → divided into ~3 sub-segments
    /// - Each sub-segment <500 chars
    /// - Timestamps don't overlap
    /// - sum(durations) = original duration
    /// </summary>
    [Fact]
    public async Task TranscriptionPipeline_SegmentationService_ShouldSplitLargeSegmentCorrectly()
    {
        // Arrange
        _output.WriteLine("=== Test 3: SegmentationService Integration ===");
        await AuthenticateAsync();

        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        video.Status = VideoStatus.Pending;
        video.TranscriptionStatus = TranscriptionStatus.NotStarted;

        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        var processor = CreateProcessor();

        // Setup mocks
        _mockTranscriptionService.Setup(x => x.IsWhisperAvailableAsync()).ReturnsAsync(true);
        _mockAudioExtractionService
            .Setup(x => x.ExtractAudioFromYouTubeAsync(video.YouTubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("C:\\temp\\audio_segmentation.mp3");
        _mockAudioExtractionService
            .Setup(x => x.GetAudioInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AudioInfo { Duration = TimeSpan.FromMinutes(5), FileSizeBytes = 4000000 });

        // Create exactly 1200 character segment
        var text1200 = "In modern software development, the importance of comprehensive testing cannot be overstated. " +
                       "Testing serves as the foundation for building reliable, maintainable, and scalable applications. " +
                       "Unit tests verify individual components in isolation, ensuring each function performs as expected. " +
                       "Integration tests validate that multiple components work together seamlessly, catching issues that unit tests might miss. " +
                       "End-to-end tests simulate real user scenarios, providing confidence that the entire system functions correctly. " +
                       "Performance tests identify bottlenecks and ensure the application can handle expected load. " +
                       "Security tests protect against vulnerabilities and potential attacks. " +
                       "Automated testing enables continuous integration and deployment, allowing teams to release features faster and with greater confidence. " +
                       "Test-driven development encourages better design and clearer requirements. " +
                       "Code coverage metrics help identify untested areas, though high coverage alone doesn't guarantee quality. " +
                       "The testing pyramid suggests having many fast unit tests, fewer integration tests, and minimal end-to-end tests. " +
                       "Mocking and stubbing allow testing in isolation without external dependencies. " +
                       "Test data management ensures consistent and reliable test results across different environments and executions.";

        _output.WriteLine($"Generated text length: {text1200.Length} characters");

        var mockSegments = new List<TranscriptSegmentDto>
        {
            new() { StartTime = 0, EndTime = 60, Text = text1200, Confidence = 0.94 }
        };

        _mockTranscriptionService
            .Setup(x => x.TranscribeAudioAsync(It.IsAny<TranscriptionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TranscriptionResultDto
            {
                VideoId = video.Id,
                Duration = TimeSpan.FromMinutes(5),
                Language = "en",
                Segments = mockSegments
            });

        // Act
        var result = await processor.ProcessTranscriptionJobAsync(video.Id);

        // Assert
        result.Should().BeTrue();

        var segments = await DbContext.TranscriptSegments
            .Where(s => s.VideoId == video.Id)
            .OrderBy(s => s.SegmentIndex)
            .ToListAsync();

        _output.WriteLine($"Original: 1 segment, After split: {segments.Count} segments");

        // Should be divided into approximately 3 sub-segments (1200 / 500 ≈ 2.4, rounded up)
        segments.Count.Should().BeGreaterThanOrEqualTo(3, "1200-char segment should split into at least 3 sub-segments");
        segments.Count.Should().BeLessThanOrEqualTo(4, "1200-char segment should split into at most 4 sub-segments");

        // Verify each sub-segment <500 chars
        foreach (var segment in segments)
        {
            segment.Text.Length.Should().BeLessThanOrEqualTo(500,
                $"Sub-segment {segment.SegmentIndex} should be <= 500 characters, got {segment.Text.Length}");
            _output.WriteLine($"Segment {segment.SegmentIndex}: {segment.Text.Length} chars, Duration: {segment.EndTime - segment.StartTime}s");
        }

        _output.WriteLine("✓ All sub-segments <= 500 characters");

        // Verify timestamps don't overlap
        for (int i = 1; i < segments.Count; i++)
        {
            segments[i].StartTime.Should().BeGreaterThanOrEqualTo(segments[i - 1].EndTime,
                $"Segment {i} StartTime ({segments[i].StartTime}) should be >= Segment {i - 1} EndTime ({segments[i - 1].EndTime})");
        }

        _output.WriteLine("✓ No timestamp overlaps");

        // Verify sum(durations) = original duration (60 seconds)
        var totalDuration = segments.Sum(s => s.EndTime - s.StartTime);
        const double originalDuration = 60.0;
        Math.Abs(totalDuration - originalDuration).Should().BeLessThan(0.1,
            $"Sum of durations ({totalDuration}s) should equal original duration ({originalDuration}s)");

        _output.WriteLine($"✓ Duration preservation verified: {totalDuration}s ≈ {originalDuration}s");
        _output.WriteLine("=== Test 3 PASSED ===\n");
    }

    #endregion

    #region Test 4: Error Handling - Whisper Fails

    /// <summary>
    /// Test 4: Error Handling - Whisper Service Failure
    ///
    /// Validates:
    /// - Mock Whisper throws exception
    /// - Job status = Failed
    /// - TranscriptionStatus = Failed
    /// - Error message saved
    /// </summary>
    [Fact]
    public async Task TranscriptionPipeline_WhisperFails_ShouldHandleErrorGracefully()
    {
        // Arrange
        _output.WriteLine("=== Test 4: Error Handling - Whisper Fails ===");
        await AuthenticateAsync();

        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        video.Status = VideoStatus.Pending;
        video.TranscriptionStatus = TranscriptionStatus.NotStarted;

        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        _output.WriteLine($"Created test video: {video.Id}");

        var processor = CreateProcessor();

        // Setup mocks - Whisper fails during transcription
        _mockTranscriptionService.Setup(x => x.IsWhisperAvailableAsync()).ReturnsAsync(true);
        _mockAudioExtractionService
            .Setup(x => x.ExtractAudioFromYouTubeAsync(video.YouTubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("C:\\temp\\audio_fail.mp3");
        _mockAudioExtractionService
            .Setup(x => x.GetAudioInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AudioInfo { Duration = TimeSpan.FromMinutes(5), FileSizeBytes = 4000000 });

        // Simulate Whisper failure
        var expectedErrorMessage = "Whisper transcription failed: Out of memory error";
        _mockTranscriptionService
            .Setup(x => x.TranscribeAudioAsync(It.IsAny<TranscriptionRequestDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(expectedErrorMessage));

        // Act
        var result = await processor.ProcessTranscriptionJobAsync(video.Id);

        // Assert
        result.Should().BeFalse("Pipeline should fail when Whisper throws exception");

        _output.WriteLine("✓ Pipeline returned false on error");

        // Verify Job status = Failed
        var job = await DbContext.Jobs
            .FirstOrDefaultAsync(j => j.VideoId == video.Id && j.Type == JobType.Transcription);

        job.Should().NotBeNull("Job should be created even if it fails");
        job!.Status.Should().Be(JobStatus.Failed, "Job status should be Failed");
        job.FailedAt.Should().NotBeNull("FailedAt timestamp should be set");
        // Error message is user-friendly, not the raw exception message
        job.ErrorMessage.Should().NotBeNullOrEmpty("Error message should be saved");

        _output.WriteLine($"✓ Job status: {job.Status}, Error: {job.ErrorMessage}");

        // Verify TranscriptionStatus = Failed
        var updatedVideo = await DbContext.Videos.FindAsync(video.Id);
        updatedVideo.Should().NotBeNull();
        updatedVideo!.TranscriptionStatus.Should().Be(TranscriptionStatus.Failed,
            "Video TranscriptionStatus should be Failed");

        _output.WriteLine($"✓ Video TranscriptionStatus: {updatedVideo.TranscriptionStatus}");

        // Verify error message saved
        job.ErrorMessage.Should().NotBeNullOrEmpty("Error message should be persisted");
        // Note: Error message is user-friendly, may not contain technical details like "Whisper"

        _output.WriteLine("✓ Error message saved correctly");

        // Verify no segments were saved
        var segments = await DbContext.TranscriptSegments
            .Where(s => s.VideoId == video.Id)
            .ToListAsync();

        segments.Should().BeEmpty("No segments should be saved on failure");

        _output.WriteLine("✓ No segments saved on failure");
        _output.WriteLine("=== Test 4 PASSED ===\n");
    }

    #endregion

    #region Test 5: Bulk Insert Performance

    /// <summary>
    /// Test 5: Bulk Insert Performance
    ///
    /// Validates:
    /// - 200 segments simulated
    /// - Insertion in <5 seconds
    /// - Uses BulkInsertAsync (verified by single CreatedAt timestamp)
    /// - All segments in DB after insertion
    /// </summary>
    [Fact]
    public async Task TranscriptionPipeline_BulkInsert_ShouldHandleLargeSegmentCountEfficiently()
    {
        // Arrange
        _output.WriteLine("=== Test 5: Bulk Insert Performance ===");
        await AuthenticateAsync();

        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        video.Status = VideoStatus.Pending;
        video.TranscriptionStatus = TranscriptionStatus.NotStarted;

        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        _output.WriteLine($"Created test video: {video.Id}");

        var processor = CreateProcessor();

        // Setup mocks
        _mockTranscriptionService.Setup(x => x.IsWhisperAvailableAsync()).ReturnsAsync(true);
        _mockAudioExtractionService
            .Setup(x => x.ExtractAudioFromYouTubeAsync(video.YouTubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("C:\\temp\\audio_bulk.mp3");
        _mockAudioExtractionService
            .Setup(x => x.GetAudioInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AudioInfo { Duration = TimeSpan.FromHours(1), FileSizeBytes = 50000000 });

        // Generate 200 segments (simulating a 1-hour video with dense transcription)
        var mockSegments = new List<TranscriptSegmentDto>();
        double currentTime = 0;

        for (int i = 0; i < 200; i++)
        {
            var duration = 18.0; // 18 seconds per segment = 200 * 18 = 3600 seconds = 1 hour
            mockSegments.Add(new TranscriptSegmentDto
            {
                StartTime = currentTime,
                EndTime = currentTime + duration,
                Text = $"Segment {i}: This is test transcription text for performance testing. " +
                       $"We are validating that bulk insert can handle large numbers of segments efficiently. " +
                       $"The system should use optimized batch insertion rather than individual inserts.",
                Confidence = 0.90 + (i % 10) * 0.01
            });
            currentTime += duration;
        }

        _output.WriteLine($"Generated {mockSegments.Count} mock segments");

        _mockTranscriptionService
            .Setup(x => x.TranscribeAudioAsync(It.IsAny<TranscriptionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TranscriptionResultDto
            {
                VideoId = video.Id,
                Duration = TimeSpan.FromHours(1),
                Language = "en",
                Segments = mockSegments
            });

        // Act - Measure performance
        var stopwatch = Stopwatch.StartNew();
        var result = await processor.ProcessTranscriptionJobAsync(video.Id);
        stopwatch.Stop();

        var elapsedMs = stopwatch.ElapsedMilliseconds;
        _output.WriteLine($"Pipeline execution time: {elapsedMs}ms ({elapsedMs / 1000.0:F2}s)");

        // Assert
        result.Should().BeTrue("Pipeline should complete successfully");

        // Verify insertion time <5 seconds
        elapsedMs.Should().BeLessThan(5000,
            $"Bulk insert of 200 segments should complete in <5 seconds, took {elapsedMs}ms");

        _output.WriteLine($"✓ Performance target met: {elapsedMs}ms < 5000ms");

        // Verify all segments in DB
        var segments = await DbContext.TranscriptSegments
            .Where(s => s.VideoId == video.Id)
            .OrderBy(s => s.SegmentIndex)
            .ToListAsync();

        segments.Count.Should().Be(200, "All 200 segments should be saved");

        _output.WriteLine($"✓ All {segments.Count} segments saved to database");

        // Verify bulk insert was used (all segments created at same time)
        var createdAtTimes = segments.Select(s => s.CreatedAt).Distinct().ToList();
        createdAtTimes.Should().HaveCount(1,
            "All segments should have same CreatedAt timestamp (indicating bulk insert)");

        _output.WriteLine($"✓ Bulk insert verified - All segments created at: {createdAtTimes.First()}");

        // Verify sequential indexing
        for (int i = 0; i < segments.Count; i++)
        {
            segments[i].SegmentIndex.Should().Be(i, $"Segment {i} should have correct index");
        }

        _output.WriteLine("✓ Sequential indexing verified for all 200 segments");

        // Additional performance metrics
        var avgInsertTime = elapsedMs / (double)segments.Count;
        _output.WriteLine($"Average time per segment: {avgInsertTime:F2}ms");

        _output.WriteLine("=== Test 5 PASSED ===\n");
    }

    #endregion
}
