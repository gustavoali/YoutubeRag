using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using YoutubeRag.Application.Configuration;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Application.Interfaces.Services;
using YoutubeRag.Application.Services;
using YoutubeRag.Domain.Entities;

namespace YoutubeRag.Tests.Integration.Unit;

/// <summary>
/// Unit tests for segment integrity validation in TranscriptionJobProcessor
/// Tests validation of segment indexes, timestamps, overlaps, and VideoId consistency
/// </summary>
public class SegmentIntegrityValidationUnitTests
{
    private readonly Mock<IVideoRepository> _mockVideoRepository;
    private readonly Mock<IJobRepository> _mockJobRepository;
    private readonly Mock<ITranscriptSegmentRepository> _mockTranscriptSegmentRepository;
    private readonly Mock<IDeadLetterJobRepository> _mockDeadLetterJobRepository;
    private readonly Mock<IAudioExtractionService> _mockAudioExtractionService;
    private readonly Mock<IVideoDownloadService> _mockVideoDownloadService;
    private readonly Mock<ITranscriptionService> _mockTranscriptionService;
    private readonly Mock<ISegmentationService> _mockSegmentationService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IAppConfiguration> _mockAppConfiguration;
    private readonly Mock<IBackgroundJobService> _mockBackgroundJobService;
    private readonly Mock<IProgressNotificationService> _mockProgressNotificationService;
    private readonly Mock<ILogger<TranscriptionJobProcessor>> _mockLogger;

    public SegmentIntegrityValidationUnitTests()
    {
        _mockVideoRepository = new Mock<IVideoRepository>();
        _mockJobRepository = new Mock<IJobRepository>();
        _mockTranscriptSegmentRepository = new Mock<ITranscriptSegmentRepository>();
        _mockDeadLetterJobRepository = new Mock<IDeadLetterJobRepository>();
        _mockAudioExtractionService = new Mock<IAudioExtractionService>();
        _mockVideoDownloadService = new Mock<IVideoDownloadService>();
        _mockTranscriptionService = new Mock<ITranscriptionService>();
        _mockSegmentationService = new Mock<ISegmentationService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockAppConfiguration = new Mock<IAppConfiguration>();
        _mockBackgroundJobService = new Mock<IBackgroundJobService>();
        _mockProgressNotificationService = new Mock<IProgressNotificationService>();
        _mockLogger = new Mock<ILogger<TranscriptionJobProcessor>>();

        // Default configuration
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
            _mockVideoRepository.Object,
            _mockJobRepository.Object,
            _mockTranscriptSegmentRepository.Object,
            _mockDeadLetterJobRepository.Object,
            _mockAudioExtractionService.Object,
            _mockVideoDownloadService.Object,
            _mockTranscriptionService.Object,
            _mockSegmentationService.Object,
            _mockUnitOfWork.Object,
            _mockAppConfiguration.Object,
            _mockBackgroundJobService.Object,
            _mockProgressNotificationService.Object,
            _mockLogger.Object
        );
    }

    #region ValidateSegmentIntegrity Tests

    [Fact]
    public void ValidateSegmentIntegrity_ValidSegments_PassesWithoutWarnings()
    {
        // Arrange
        var processor = CreateProcessor();
        var videoId = "test-video-id";

        var segments = new List<TranscriptSegment>
        {
            new TranscriptSegment
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = videoId,
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
                VideoId = videoId,
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
                VideoId = videoId,
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

        var exception = Record.Exception(() => method!.Invoke(processor, new object[] { segments, videoId }));

        // Assert
        exception.Should().BeNull("valid segments should pass validation without errors");
    }

    [Fact]
    public void ValidateSegmentIntegrity_GapInSegmentIndex_LogsWarning()
    {
        // Arrange
        var processor = CreateProcessor();
        var videoId = "test-video-id";

        var segments = new List<TranscriptSegment>
        {
            new TranscriptSegment
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = videoId,
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
                VideoId = videoId,
                SegmentIndex = 5, // Gap: should be 1
                StartTime = 5,
                EndTime = 10,
                Text = "Second segment",
                Language = "en",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Act
        var method = typeof(TranscriptionJobProcessor).GetMethod("ValidateSegmentIntegrity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var exception = Record.Exception(() => method!.Invoke(processor, new object[] { segments, videoId }));

        // Assert - Should log warning but not throw
        exception.Should().BeNull("gap in segment index should log warning but not throw");
    }

    [Fact]
    public void ValidateSegmentIntegrity_TimestampsNotIncreasing_LogsWarning()
    {
        // Arrange
        var processor = CreateProcessor();
        var videoId = "test-video-id";

        var segments = new List<TranscriptSegment>
        {
            new TranscriptSegment
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = videoId,
                SegmentIndex = 0,
                StartTime = 10,
                EndTime = 15,
                Text = "First segment",
                Language = "en",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new TranscriptSegment
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = videoId,
                SegmentIndex = 1,
                StartTime = 5, // Earlier than previous
                EndTime = 10,
                Text = "Second segment",
                Language = "en",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Act
        var method = typeof(TranscriptionJobProcessor).GetMethod("ValidateSegmentIntegrity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var exception = Record.Exception(() => method!.Invoke(processor, new object[] { segments, videoId }));

        // Assert - Should log warning but not throw
        exception.Should().BeNull("non-increasing timestamps should log warning but not throw");
    }

    [Fact]
    public void ValidateSegmentIntegrity_OverlappingSegments_LogsWarning()
    {
        // Arrange
        var processor = CreateProcessor();
        var videoId = "test-video-id";

        var segments = new List<TranscriptSegment>
        {
            new TranscriptSegment
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = videoId,
                SegmentIndex = 0,
                StartTime = 0,
                EndTime = 8, // Overlaps with next
                Text = "First segment",
                Language = "en",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new TranscriptSegment
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = videoId,
                SegmentIndex = 1,
                StartTime = 5, // Overlap: 8 > 5
                EndTime = 10,
                Text = "Second segment",
                Language = "en",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Act
        var method = typeof(TranscriptionJobProcessor).GetMethod("ValidateSegmentIntegrity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var exception = Record.Exception(() => method!.Invoke(processor, new object[] { segments, videoId }));

        // Assert - Should log warning but not throw
        exception.Should().BeNull("overlapping segments should log warning but not throw");
    }

    [Fact]
    public void ValidateSegmentIntegrity_InvalidVideoId_ThrowsException()
    {
        // Arrange
        var processor = CreateProcessor();
        var videoId = "test-video-id";

        var segments = new List<TranscriptSegment>
        {
            new TranscriptSegment
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = "", // Invalid
                SegmentIndex = 0,
                StartTime = 0,
                EndTime = 5,
                Text = "First segment",
                Language = "en",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Act
        var method = typeof(TranscriptionJobProcessor).GetMethod("ValidateSegmentIntegrity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var exception = Record.Exception(() => method!.Invoke(processor, new object[] { segments, videoId }));

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<System.Reflection.TargetInvocationException>();
        exception.InnerException.Should().BeOfType<InvalidOperationException>();
        exception.InnerException!.Message.Should().Contain("invalid VideoId");
    }

    [Fact]
    public void ValidateSegmentIntegrity_MismatchedVideoId_ThrowsException()
    {
        // Arrange
        var processor = CreateProcessor();
        var videoId = "test-video-id";
        var wrongVideoId = "different-video-id";

        var segments = new List<TranscriptSegment>
        {
            new TranscriptSegment
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = wrongVideoId, // Mismatched
                SegmentIndex = 0,
                StartTime = 0,
                EndTime = 5,
                Text = "First segment",
                Language = "en",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Act
        var method = typeof(TranscriptionJobProcessor).GetMethod("ValidateSegmentIntegrity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var exception = Record.Exception(() => method!.Invoke(processor, new object[] { segments, videoId }));

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<System.Reflection.TargetInvocationException>();
        exception.InnerException.Should().BeOfType<InvalidOperationException>();
        exception.InnerException!.Message.Should().Contain("mismatched VideoId");
    }

    [Fact]
    public void ValidateSegmentIntegrity_NegativeTimestamps_ThrowsException()
    {
        // Arrange
        var processor = CreateProcessor();
        var videoId = "test-video-id";

        var segments = new List<TranscriptSegment>
        {
            new TranscriptSegment
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = videoId,
                SegmentIndex = 0,
                StartTime = -5, // Negative
                EndTime = 5,
                Text = "First segment",
                Language = "en",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Act
        var method = typeof(TranscriptionJobProcessor).GetMethod("ValidateSegmentIntegrity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var exception = Record.Exception(() => method!.Invoke(processor, new object[] { segments, videoId }));

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<System.Reflection.TargetInvocationException>();
        exception.InnerException.Should().BeOfType<InvalidOperationException>();
        exception.InnerException!.Message.Should().Contain("negative timestamps");
    }

    [Fact]
    public void ValidateSegmentIntegrity_EmptySegmentList_ThrowsException()
    {
        // Arrange
        var processor = CreateProcessor();
        var videoId = "test-video-id";
        var segments = new List<TranscriptSegment>();

        // Act
        var method = typeof(TranscriptionJobProcessor).GetMethod("ValidateSegmentIntegrity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var exception = Record.Exception(() => method!.Invoke(processor, new object[] { segments, videoId }));

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<System.Reflection.TargetInvocationException>();
        exception.InnerException.Should().BeOfType<ArgumentException>();
        exception.InnerException!.Message.Should().Contain("cannot be null or empty");
    }

    [Fact]
    public void ValidateSegmentIntegrity_EmptyTextSegment_LogsWarning()
    {
        // Arrange
        var processor = CreateProcessor();
        var videoId = "test-video-id";

        var segments = new List<TranscriptSegment>
        {
            new TranscriptSegment
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = videoId,
                SegmentIndex = 0,
                StartTime = 0,
                EndTime = 5,
                Text = "   ", // Empty/whitespace
                Language = "en",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Act
        var method = typeof(TranscriptionJobProcessor).GetMethod("ValidateSegmentIntegrity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var exception = Record.Exception(() => method!.Invoke(processor, new object[] { segments, videoId }));

        // Assert - Should log warning but not throw
        exception.Should().BeNull("empty text should log warning but not throw");
    }

    [Fact]
    public void ValidateSegmentIntegrity_InvalidDuration_LogsWarning()
    {
        // Arrange
        var processor = CreateProcessor();
        var videoId = "test-video-id";

        var segments = new List<TranscriptSegment>
        {
            new TranscriptSegment
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = videoId,
                SegmentIndex = 0,
                StartTime = 10,
                EndTime = 10, // EndTime equals StartTime
                Text = "Invalid duration segment",
                Language = "en",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Act
        var method = typeof(TranscriptionJobProcessor).GetMethod("ValidateSegmentIntegrity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var exception = Record.Exception(() => method!.Invoke(processor, new object[] { segments, videoId }));

        // Assert - Should log warning but not throw
        exception.Should().BeNull("invalid duration should log warning but not throw");
    }

    #endregion
}
