using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using YoutubeRag.Application.Configuration;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Application.Interfaces.Services;
using YoutubeRag.Application.Services;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Tests.Integration.Infrastructure;

namespace YoutubeRag.Tests.Integration.Services;

/// <summary>
/// Integration tests for segment integrity validation in TranscriptionJobProcessor
/// Tests validation of segment indexes, timestamps, overlaps, and VideoId consistency
/// </summary>
public class SegmentIntegrityValidationTests : IntegrationTestBase
{
    private readonly Mock<IAudioExtractionService> _mockAudioExtractionService;
    private readonly Mock<IVideoDownloadService> _mockVideoDownloadService;
    private readonly Mock<ITranscriptionService> _mockTranscriptionService;
    private readonly Mock<IAppConfiguration> _mockAppConfiguration;
    private readonly Mock<IProgressNotificationService> _mockProgressNotificationService;

    public SegmentIntegrityValidationTests(CustomWebApplicationFactory<Program> factory) : base(factory)
    {
        _mockAudioExtractionService = new Mock<IAudioExtractionService>();
        _mockVideoDownloadService = new Mock<IVideoDownloadService>();
        _mockTranscriptionService = new Mock<ITranscriptionService>();
        _mockAppConfiguration = new Mock<IAppConfiguration>();
        _mockProgressNotificationService = new Mock<IProgressNotificationService>();

        // Default mock configuration
        _mockAppConfiguration.Setup(x => x.AutoGenerateEmbeddings).Returns(false);

        // Default mock for video download service
        _mockVideoDownloadService
            .Setup(x => x.DownloadVideoAsync(It.IsAny<string>(), It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string youtubeId, IProgress<double> progress, CancellationToken ct) =>
                $"C:\\temp\\{youtubeId}_video.mp4");
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

    #region ValidateSegmentIntegrity Tests

    [Fact]
    public async Task ValidateSegmentIntegrity_ValidSegments_PassesWithoutWarnings()
    {
        // Arrange
        await AuthenticateAsync();
        var video = Helpers.TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        var processor = CreateProcessor();

        // Setup valid segments with proper sequencing
        var segments = new List<TranscriptSegment>
        {
            new TranscriptSegment
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = video.Id,
                SegmentIndex = 0,
                StartTime = 0,
                EndTime = 5,
                Text = "First segment",
                Language = "en",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new TranscriptSegment
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = video.Id,
                SegmentIndex = 1,
                StartTime = 5,
                EndTime = 10,
                Text = "Second segment",
                Language = "en",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new TranscriptSegment
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = video.Id,
                SegmentIndex = 2,
                StartTime = 10,
                EndTime = 15,
                Text = "Third segment",
                Language = "en",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Act - Use reflection to invoke private method
        var method = typeof(TranscriptionJobProcessor).GetMethod("ValidateSegmentIntegrity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var exception = Record.Exception(() => method!.Invoke(processor, new object[] { segments, video.Id }));

        // Assert
        exception.Should().BeNull("valid segments should pass validation without errors");
    }

    [Fact]
    public async Task ValidateSegmentIntegrity_GapInSegmentIndex_LogsWarning()
    {
        // Arrange
        await AuthenticateAsync();
        var video = Helpers.TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        var processor = CreateProcessor();

        // Segments with gap in SegmentIndex
        var segments = new List<TranscriptSegment>
        {
            new TranscriptSegment
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = video.Id,
                SegmentIndex = 0,
                StartTime = 0,
                EndTime = 5,
                Text = "First segment",
                Language = "en",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new TranscriptSegment
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = video.Id,
                SegmentIndex = 5, // Gap: should be 1, not 5
                StartTime = 5,
                EndTime = 10,
                Text = "Second segment",
                Language = "en",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Act - Use reflection to invoke private method
        var method = typeof(TranscriptionJobProcessor).GetMethod("ValidateSegmentIntegrity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var exception = Record.Exception(() => method!.Invoke(processor, new object[] { segments, video.Id }));

        // Assert - Should log warning but not throw exception
        exception.Should().BeNull("gap in segment index should log warning but not throw");
    }

    [Fact]
    public async Task ValidateSegmentIntegrity_TimestampsNotIncreasing_LogsWarning()
    {
        // Arrange
        await AuthenticateAsync();
        var video = Helpers.TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        var processor = CreateProcessor();

        // Segments with non-increasing timestamps
        var segments = new List<TranscriptSegment>
        {
            new TranscriptSegment
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = video.Id,
                SegmentIndex = 0,
                StartTime = 10, // Later start time
                EndTime = 15,
                Text = "First segment",
                Language = "en",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new TranscriptSegment
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = video.Id,
                SegmentIndex = 1,
                StartTime = 5, // Earlier start time - violation
                EndTime = 10,
                Text = "Second segment",
                Language = "en",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Act - Use reflection to invoke private method
        var method = typeof(TranscriptionJobProcessor).GetMethod("ValidateSegmentIntegrity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var exception = Record.Exception(() => method!.Invoke(processor, new object[] { segments, video.Id }));

        // Assert - Should log warning but not throw exception
        exception.Should().BeNull("non-increasing timestamps should log warning but not throw");
    }

    [Fact]
    public async Task ValidateSegmentIntegrity_OverlappingSegments_LogsWarning()
    {
        // Arrange
        await AuthenticateAsync();
        var video = Helpers.TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        var processor = CreateProcessor();

        // Segments with overlap: EndTime[0] > StartTime[1]
        var segments = new List<TranscriptSegment>
        {
            new TranscriptSegment
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = video.Id,
                SegmentIndex = 0,
                StartTime = 0,
                EndTime = 8, // Overlaps with next segment
                Text = "First segment",
                Language = "en",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new TranscriptSegment
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = video.Id,
                SegmentIndex = 1,
                StartTime = 5, // Overlap: 8 > 5
                EndTime = 10,
                Text = "Second segment",
                Language = "en",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Act - Use reflection to invoke private method
        var method = typeof(TranscriptionJobProcessor).GetMethod("ValidateSegmentIntegrity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var exception = Record.Exception(() => method!.Invoke(processor, new object[] { segments, video.Id }));

        // Assert - Should log warning but not throw exception
        exception.Should().BeNull("overlapping segments should log warning but not throw");
    }

    [Fact]
    public async Task ValidateSegmentIntegrity_InvalidVideoId_ThrowsException()
    {
        // Arrange
        await AuthenticateAsync();
        var video = Helpers.TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        var processor = CreateProcessor();

        // Segment with null/empty VideoId
        var segments = new List<TranscriptSegment>
        {
            new TranscriptSegment
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = "", // Invalid: empty string
                SegmentIndex = 0,
                StartTime = 0,
                EndTime = 5,
                Text = "First segment",
                Language = "en",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Act - Use reflection to invoke private method
        var method = typeof(TranscriptionJobProcessor).GetMethod("ValidateSegmentIntegrity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var exception = Record.Exception(() => method!.Invoke(processor, new object[] { segments, video.Id }));

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<System.Reflection.TargetInvocationException>();
        exception.InnerException.Should().BeOfType<InvalidOperationException>();
        exception.InnerException!.Message.Should().Contain("invalid VideoId");
    }

    [Fact]
    public async Task ValidateSegmentIntegrity_MismatchedVideoId_ThrowsException()
    {
        // Arrange
        await AuthenticateAsync();
        var video = Helpers.TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        var processor = CreateProcessor();

        // Segment with different VideoId
        var wrongVideoId = "different-video-id";
        var segments = new List<TranscriptSegment>
        {
            new TranscriptSegment
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = wrongVideoId, // Mismatched VideoId
                SegmentIndex = 0,
                StartTime = 0,
                EndTime = 5,
                Text = "First segment",
                Language = "en",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Act - Use reflection to invoke private method
        var method = typeof(TranscriptionJobProcessor).GetMethod("ValidateSegmentIntegrity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var exception = Record.Exception(() => method!.Invoke(processor, new object[] { segments, video.Id }));

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<System.Reflection.TargetInvocationException>();
        exception.InnerException.Should().BeOfType<InvalidOperationException>();
        exception.InnerException!.Message.Should().Contain("mismatched VideoId");
    }

    [Fact]
    public async Task ValidateSegmentIntegrity_NegativeTimestamps_ThrowsException()
    {
        // Arrange
        await AuthenticateAsync();
        var video = Helpers.TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        var processor = CreateProcessor();

        // Segment with negative timestamps
        var segments = new List<TranscriptSegment>
        {
            new TranscriptSegment
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = video.Id,
                SegmentIndex = 0,
                StartTime = -5, // Negative timestamp
                EndTime = 5,
                Text = "First segment",
                Language = "en",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Act - Use reflection to invoke private method
        var method = typeof(TranscriptionJobProcessor).GetMethod("ValidateSegmentIntegrity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var exception = Record.Exception(() => method!.Invoke(processor, new object[] { segments, video.Id }));

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<System.Reflection.TargetInvocationException>();
        exception.InnerException.Should().BeOfType<InvalidOperationException>();
        exception.InnerException!.Message.Should().Contain("negative timestamps");
    }

    [Fact]
    public async Task ValidateSegmentIntegrity_EmptySegmentList_ThrowsException()
    {
        // Arrange
        await AuthenticateAsync();
        var video = Helpers.TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        var processor = CreateProcessor();

        // Empty segment list
        var segments = new List<TranscriptSegment>();

        // Act - Use reflection to invoke private method
        var method = typeof(TranscriptionJobProcessor).GetMethod("ValidateSegmentIntegrity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var exception = Record.Exception(() => method!.Invoke(processor, new object[] { segments, video.Id }));

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<System.Reflection.TargetInvocationException>();
        exception.InnerException.Should().BeOfType<ArgumentException>();
        exception.InnerException!.Message.Should().Contain("cannot be null or empty");
    }

    [Fact]
    public async Task ValidateSegmentIntegrity_EmptyTextSegment_LogsWarning()
    {
        // Arrange
        await AuthenticateAsync();
        var video = Helpers.TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        var processor = CreateProcessor();

        // Segment with empty text
        var segments = new List<TranscriptSegment>
        {
            new TranscriptSegment
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = video.Id,
                SegmentIndex = 0,
                StartTime = 0,
                EndTime = 5,
                Text = "   ", // Empty/whitespace text
                Language = "en",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Act - Use reflection to invoke private method
        var method = typeof(TranscriptionJobProcessor).GetMethod("ValidateSegmentIntegrity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var exception = Record.Exception(() => method!.Invoke(processor, new object[] { segments, video.Id }));

        // Assert - Should log warning but not throw exception
        exception.Should().BeNull("empty text should log warning but not throw");
    }

    [Fact]
    public async Task ValidateSegmentIntegrity_InvalidDuration_LogsWarning()
    {
        // Arrange
        await AuthenticateAsync();
        var video = Helpers.TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        var processor = CreateProcessor();

        // Segment with EndTime <= StartTime
        var segments = new List<TranscriptSegment>
        {
            new TranscriptSegment
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = video.Id,
                SegmentIndex = 0,
                StartTime = 10,
                EndTime = 10, // EndTime equals StartTime
                Text = "Invalid duration segment",
                Language = "en",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Act - Use reflection to invoke private method
        var method = typeof(TranscriptionJobProcessor).GetMethod("ValidateSegmentIntegrity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var exception = Record.Exception(() => method!.Invoke(processor, new object[] { segments, video.Id }));

        // Assert - Should log warning but not throw exception
        exception.Should().BeNull("invalid duration should log warning but not throw");
    }

    #endregion
}
