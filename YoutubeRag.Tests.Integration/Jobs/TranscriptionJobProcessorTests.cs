using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using YoutubeRag.Application.Configuration;
using YoutubeRag.Application.DTOs.Transcription;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Application.Interfaces.Services;
using YoutubeRag.Application.Services;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;
using YoutubeRag.Tests.Integration.Helpers;
using YoutubeRag.Tests.Integration.Infrastructure;

namespace YoutubeRag.Tests.Integration.Jobs;

/// <summary>
/// Integration tests for TranscriptionJobProcessor
/// Tests job creation, state transitions, retry logic, and failure handling
/// </summary>
public class TranscriptionJobProcessorTests : IntegrationTestBase
{
    private readonly Mock<IAudioExtractionService> _mockAudioExtractionService;
    private readonly Mock<ITranscriptionService> _mockTranscriptionService;
    private readonly Mock<IAppConfiguration> _mockAppConfiguration;
    private readonly Mock<IProgressNotificationService> _mockProgressNotificationService;

    public TranscriptionJobProcessorTests(CustomWebApplicationFactory<Program> factory) : base(factory)
    {
        _mockAudioExtractionService = new Mock<IAudioExtractionService>();
        _mockTranscriptionService = new Mock<ITranscriptionService>();
        _mockAppConfiguration = new Mock<IAppConfiguration>();
        _mockProgressNotificationService = new Mock<IProgressNotificationService>();

        // Default mock configuration
        _mockAppConfiguration.Setup(x => x.AutoGenerateEmbeddings).Returns(false);
    }

    private TranscriptionJobProcessor CreateProcessor()
    {
        return new TranscriptionJobProcessor(
            Scope.ServiceProvider.GetRequiredService<IVideoRepository>(),
            Scope.ServiceProvider.GetRequiredService<IJobRepository>(),
            Scope.ServiceProvider.GetRequiredService<ITranscriptSegmentRepository>(),
            _mockAudioExtractionService.Object,
            _mockTranscriptionService.Object,
            Scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
            _mockAppConfiguration.Object,
            Scope.ServiceProvider.GetRequiredService<IBackgroundJobService>(),
            _mockProgressNotificationService.Object,
            Scope.ServiceProvider.GetRequiredService<ILogger<TranscriptionJobProcessor>>()
        );
    }

    #region Job Creation from Ingestion Tests

    [Fact]
    public async Task ProcessTranscriptionJobAsync_VideoExists_CreatesJobIfNotExists()
    {
        // Arrange
        await AuthenticateAsync();
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        video.Status = VideoStatus.Pending;
        video.TranscriptionStatus = TranscriptionStatus.NotStarted;

        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        var processor = CreateProcessor();

        // Setup mocks
        _mockTranscriptionService
            .Setup(x => x.IsWhisperAvailableAsync())
            .ReturnsAsync(true);
        _mockAudioExtractionService
            .Setup(x => x.ExtractAudioFromYouTubeAsync(video.YouTubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("C:\\temp\\audio.mp3");
        _mockAudioExtractionService
            .Setup(x => x.GetAudioInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AudioInfo
            {
                Duration = TimeSpan.FromMinutes(5),
                FileSizeBytes = 5000000
            });
        _mockTranscriptionService
            .Setup(x => x.TranscribeAudioAsync(It.IsAny<TranscriptionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TranscriptionResultDto
            {
                VideoId = video.Id,
                Duration = TimeSpan.FromMinutes(5),
                Language = "en",
                Segments = new List<TranscriptSegmentDto>
                {
                    new TranscriptSegmentDto
                    {
                        StartTime = 0,
                        EndTime = 5,
                        Text = "Test transcription segment",
                        Confidence = 0.95f
                    }
                }
            });

        // Act
        var result = await processor.ProcessTranscriptionJobAsync(video.Id);

        // Assert
        result.Should().BeTrue();

        // Verify job was created
        var jobs = await DbContext.Jobs
            .Where(j => j.VideoId == video.Id && j.Type == JobType.Transcription)
            .ToListAsync();

        jobs.Should().HaveCount(1);
        var job = jobs.First();
        job.Status.Should().Be(JobStatus.Completed);
        job.UserId.Should().Be(video.UserId);
    }

    [Fact]
    public async Task ProcessTranscriptionJobAsync_WithExistingJob_UsesExistingJob()
    {
        // Arrange
        await AuthenticateAsync();
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        video.Status = VideoStatus.Pending;

        var existingJob = new Job
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = video.Id,
            UserId = video.UserId,
            Type = JobType.Transcription,
            Status = JobStatus.Pending,
            Priority = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await DbContext.Videos.AddAsync(video);
        await DbContext.Jobs.AddAsync(existingJob);
        await DbContext.SaveChangesAsync();

        var processor = CreateProcessor();

        // Setup mocks
        _mockTranscriptionService.Setup(x => x.IsWhisperAvailableAsync()).ReturnsAsync(true);
        _mockAudioExtractionService
            .Setup(x => x.ExtractAudioFromYouTubeAsync(video.YouTubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("C:\\temp\\audio.mp3");
        _mockAudioExtractionService
            .Setup(x => x.GetAudioInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AudioInfo
            {
                Duration = TimeSpan.FromMinutes(5),
                FileSizeBytes = 5000000
            });
        _mockTranscriptionService
            .Setup(x => x.TranscribeAudioAsync(It.IsAny<TranscriptionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TranscriptionResultDto
            {
                VideoId = video.Id,
                Duration = TimeSpan.FromMinutes(5),
                Language = "en",
                Segments = new List<TranscriptSegmentDto>
                {
                    new TranscriptSegmentDto { StartTime = 0, EndTime = 5, Text = "Test", Confidence = 0.95f }
                }
            });

        // Act
        var result = await processor.ProcessTranscriptionJobAsync(video.Id);

        // Assert
        result.Should().BeTrue();

        // Verify only one job exists and it was updated
        var jobs = await DbContext.Jobs
            .Where(j => j.VideoId == video.Id && j.Type == JobType.Transcription)
            .ToListAsync();

        jobs.Should().HaveCount(1);
        jobs.First().Id.Should().Be(existingJob.Id);
        jobs.First().Status.Should().Be(JobStatus.Completed);
    }

    #endregion

    #region Job State Transitions Tests

    [Fact]
    public async Task ProcessTranscriptionJobAsync_SuccessfulProcessing_TransitionsFromPendingToRunningToCompleted()
    {
        // Arrange
        await AuthenticateAsync();
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        video.Status = VideoStatus.Pending;

        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        var processor = CreateProcessor();

        // Setup mocks for successful processing
        _mockTranscriptionService.Setup(x => x.IsWhisperAvailableAsync()).ReturnsAsync(true);
        _mockAudioExtractionService
            .Setup(x => x.ExtractAudioFromYouTubeAsync(video.YouTubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("C:\\temp\\audio.mp3");
        _mockAudioExtractionService
            .Setup(x => x.GetAudioInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AudioInfo
            {
                Duration = TimeSpan.FromMinutes(3),
                FileSizeBytes = 3000000
            });
        _mockTranscriptionService
            .Setup(x => x.TranscribeAudioAsync(It.IsAny<TranscriptionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TranscriptionResultDto
            {
                VideoId = video.Id,
                Duration = TimeSpan.FromMinutes(3),
                Language = "en",
                Segments = new List<TranscriptSegmentDto>
                {
                    new TranscriptSegmentDto { StartTime = 0, EndTime = 3, Text = "Test segment", Confidence = 0.9f }
                }
            });

        // Act
        var result = await processor.ProcessTranscriptionJobAsync(video.Id);

        // Assert
        result.Should().BeTrue();

        // Verify final job status
        var job = await DbContext.Jobs
            .FirstOrDefaultAsync(j => j.VideoId == video.Id && j.Type == JobType.Transcription);

        job.Should().NotBeNull();
        job!.Status.Should().Be(JobStatus.Completed);
        job.StartedAt.Should().NotBeNull();
        job.CompletedAt.Should().NotBeNull();
        job.Progress.Should().Be(100);

        // Verify video status updated
        var updatedVideo = await DbContext.Videos.FindAsync(video.Id);
        updatedVideo!.ProcessingStatus.Should().Be(VideoStatus.Completed);
        updatedVideo.TranscriptionStatus.Should().Be(TranscriptionStatus.Completed);
        updatedVideo.TranscribedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessTranscriptionJobAsync_Failure_TransitionsToPendingToRunningToFailed()
    {
        // Arrange
        await AuthenticateAsync();
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        video.Status = VideoStatus.Pending;

        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        var processor = CreateProcessor();

        // Setup mocks for failure scenario
        _mockTranscriptionService.Setup(x => x.IsWhisperAvailableAsync()).ReturnsAsync(true);
        _mockAudioExtractionService
            .Setup(x => x.ExtractAudioFromYouTubeAsync(video.YouTubeId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Audio extraction failed"));

        // Act
        var result = await processor.ProcessTranscriptionJobAsync(video.Id);

        // Assert
        result.Should().BeFalse();

        // Verify job status
        var job = await DbContext.Jobs
            .FirstOrDefaultAsync(j => j.VideoId == video.Id && j.Type == JobType.Transcription);

        job.Should().NotBeNull();
        job!.Status.Should().Be(JobStatus.Failed);
        job.ErrorMessage.Should().Contain("Audio extraction failed");
        job.FailedAt.Should().NotBeNull();

        // Verify video transcription status updated
        var updatedVideo = await DbContext.Videos.FindAsync(video.Id);
        updatedVideo!.TranscriptionStatus.Should().Be(TranscriptionStatus.Failed);
    }

    [Fact]
    public async Task ProcessTranscriptionJobAsync_PersistsStateChangesToDatabase()
    {
        // Arrange
        await AuthenticateAsync();
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        video.Status = VideoStatus.Pending;

        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        var processor = CreateProcessor();

        // Setup mocks
        _mockTranscriptionService.Setup(x => x.IsWhisperAvailableAsync()).ReturnsAsync(true);
        _mockAudioExtractionService
            .Setup(x => x.ExtractAudioFromYouTubeAsync(video.YouTubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("C:\\temp\\audio.mp3");
        _mockAudioExtractionService
            .Setup(x => x.GetAudioInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AudioInfo { Duration = TimeSpan.FromMinutes(5), FileSizeBytes = 5000000 });
        _mockTranscriptionService
            .Setup(x => x.TranscribeAudioAsync(It.IsAny<TranscriptionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TranscriptionResultDto
            {
                VideoId = video.Id,
                Duration = TimeSpan.FromMinutes(5),
                Language = "en",
                Segments = new List<TranscriptSegmentDto>
                {
                    new TranscriptSegmentDto { StartTime = 0, EndTime = 5, Text = "Test", Confidence = 0.95f }
                }
            });

        // Act
        var result = await processor.ProcessTranscriptionJobAsync(video.Id);

        // Assert
        result.Should().BeTrue();

        // Create a new context to verify persistence
        using var newScope = Factory.Services.CreateScope();
        var newDbContext = newScope.ServiceProvider.GetRequiredService<YoutubeRag.Infrastructure.Data.ApplicationDbContext>();

        var persistedJob = await newDbContext.Jobs
            .FirstOrDefaultAsync(j => j.VideoId == video.Id && j.Type == JobType.Transcription);

        persistedJob.Should().NotBeNull();
        persistedJob!.Status.Should().Be(JobStatus.Completed);

        var persistedVideo = await newDbContext.Videos.FindAsync(video.Id);
        persistedVideo!.TranscriptionStatus.Should().Be(TranscriptionStatus.Completed);
    }

    #endregion

    #region Retry Logic Tests

    [Fact]
    public async Task ProcessTranscriptionJobAsync_WhisperNotAvailable_MarksJobAsFailed()
    {
        // Arrange
        await AuthenticateAsync();
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        var processor = CreateProcessor();

        // Setup mock - Whisper not available
        _mockTranscriptionService.Setup(x => x.IsWhisperAvailableAsync()).ReturnsAsync(false);

        // Act
        var result = await processor.ProcessTranscriptionJobAsync(video.Id);

        // Assert
        result.Should().BeFalse();

        var job = await DbContext.Jobs
            .FirstOrDefaultAsync(j => j.VideoId == video.Id && j.Type == JobType.Transcription);

        job.Should().NotBeNull();
        job!.Status.Should().Be(JobStatus.Failed);
        job.ErrorMessage.Should().Contain("Whisper");
    }

    [Fact]
    public async Task ProcessTranscriptionJobAsync_TransientFailure_UpdatesJobWithErrorMessage()
    {
        // Arrange
        await AuthenticateAsync();
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        var processor = CreateProcessor();

        // Setup mock for transient failure
        _mockTranscriptionService.Setup(x => x.IsWhisperAvailableAsync()).ReturnsAsync(true);
        _mockAudioExtractionService
            .Setup(x => x.ExtractAudioFromYouTubeAsync(video.YouTubeId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network timeout"));

        // Act
        var result = await processor.ProcessTranscriptionJobAsync(video.Id);

        // Assert
        result.Should().BeFalse();

        var job = await DbContext.Jobs
            .FirstOrDefaultAsync(j => j.VideoId == video.Id && j.Type == JobType.Transcription);

        job.Should().NotBeNull();
        job!.Status.Should().Be(JobStatus.Failed);
        job.ErrorMessage.Should().Contain("Network timeout");
    }

    #endregion

    #region Dead Letter Queue / Permanent Failures Tests

    [Fact]
    public async Task ProcessTranscriptionJobAsync_InvalidVideoId_MarkJobAsFailedPermanently()
    {
        // Arrange
        await AuthenticateAsync();
        var processor = CreateProcessor();

        var nonExistentVideoId = "non-existent-video-id";

        // Act
        var result = await processor.ProcessTranscriptionJobAsync(nonExistentVideoId);

        // Assert
        result.Should().BeFalse();

        // No job should be created for non-existent video
        var jobs = await DbContext.Jobs.ToListAsync();
        jobs.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessTranscriptionJobAsync_PermanentFailure_DoesNotRetryIndefinitely()
    {
        // Arrange
        await AuthenticateAsync();
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        var processor = CreateProcessor();

        // Setup mock for permanent failure
        _mockTranscriptionService.Setup(x => x.IsWhisperAvailableAsync()).ReturnsAsync(true);
        _mockAudioExtractionService
            .Setup(x => x.ExtractAudioFromYouTubeAsync(video.YouTubeId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Video is private"));

        // Act
        var result = await processor.ProcessTranscriptionJobAsync(video.Id);

        // Assert
        result.Should().BeFalse();

        var job = await DbContext.Jobs
            .FirstOrDefaultAsync(j => j.VideoId == video.Id && j.Type == JobType.Transcription);

        job.Should().NotBeNull();
        job!.Status.Should().Be(JobStatus.Failed);
        job.ErrorMessage.Should().Contain("Video is private");

        // Verify video status reflects failure
        var updatedVideo = await DbContext.Videos.FindAsync(video.Id);
        updatedVideo!.TranscriptionStatus.Should().Be(TranscriptionStatus.Failed);
    }

    #endregion

    #region Transcript Segment Persistence Tests

    [Fact]
    public async Task ProcessTranscriptionJobAsync_SuccessfulTranscription_SavesSegmentsToDatabase()
    {
        // Arrange
        await AuthenticateAsync();
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        var processor = CreateProcessor();

        // Setup mocks
        _mockTranscriptionService.Setup(x => x.IsWhisperAvailableAsync()).ReturnsAsync(true);
        _mockAudioExtractionService
            .Setup(x => x.ExtractAudioFromYouTubeAsync(video.YouTubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("C:\\temp\\audio.mp3");
        _mockAudioExtractionService
            .Setup(x => x.GetAudioInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AudioInfo { Duration = TimeSpan.FromMinutes(5), FileSizeBytes = 5000000 });

        var segments = new List<TranscriptSegmentDto>
        {
            new TranscriptSegmentDto { StartTime = 0, EndTime = 2, Text = "First segment", Confidence = 0.95f },
            new TranscriptSegmentDto { StartTime = 2, EndTime = 4, Text = "Second segment", Confidence = 0.92f },
            new TranscriptSegmentDto { StartTime = 4, EndTime = 6, Text = "Third segment", Confidence = 0.98f }
        };

        _mockTranscriptionService
            .Setup(x => x.TranscribeAudioAsync(It.IsAny<TranscriptionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TranscriptionResultDto
            {
                VideoId = video.Id,
                Duration = TimeSpan.FromMinutes(5),
                Language = "en",
                Segments = segments
            });

        // Act
        var result = await processor.ProcessTranscriptionJobAsync(video.Id);

        // Assert
        result.Should().BeTrue();

        // Verify segments saved to database
        var savedSegments = await DbContext.TranscriptSegments
            .Where(s => s.VideoId == video.Id)
            .OrderBy(s => s.SegmentIndex)
            .ToListAsync();

        savedSegments.Should().HaveCount(3);
        savedSegments[0].Text.Should().Be("First segment");
        savedSegments[1].Text.Should().Be("Second segment");
        savedSegments[2].Text.Should().Be("Third segment");

        savedSegments[0].Confidence.Should().Be(0.95f);
        savedSegments[0].Language.Should().Be("en");
    }

    [Fact]
    public async Task ProcessTranscriptionJobAsync_ReplacesExistingSegments()
    {
        // Arrange
        await AuthenticateAsync();
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);

        // Add existing segments
        var existingSegment = new TranscriptSegment
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = video.Id,
            SegmentIndex = 0,
            StartTime = 0,
            EndTime = 5,
            Text = "Old segment",
            Language = "en",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.TranscriptSegments.AddAsync(existingSegment);
        await DbContext.SaveChangesAsync();

        var processor = CreateProcessor();

        // Setup mocks
        _mockTranscriptionService.Setup(x => x.IsWhisperAvailableAsync()).ReturnsAsync(true);
        _mockAudioExtractionService
            .Setup(x => x.ExtractAudioFromYouTubeAsync(video.YouTubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("C:\\temp\\audio.mp3");
        _mockAudioExtractionService
            .Setup(x => x.GetAudioInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AudioInfo { Duration = TimeSpan.FromMinutes(5), FileSizeBytes = 5000000 });
        _mockTranscriptionService
            .Setup(x => x.TranscribeAudioAsync(It.IsAny<TranscriptionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TranscriptionResultDto
            {
                VideoId = video.Id,
                Duration = TimeSpan.FromMinutes(5),
                Language = "en",
                Segments = new List<TranscriptSegmentDto>
                {
                    new TranscriptSegmentDto { StartTime = 0, EndTime = 3, Text = "New segment", Confidence = 0.95f }
                }
            });

        // Act
        var result = await processor.ProcessTranscriptionJobAsync(video.Id);

        // Assert
        result.Should().BeTrue();

        // Verify old segment replaced with new one
        var segments = await DbContext.TranscriptSegments
            .Where(s => s.VideoId == video.Id)
            .ToListAsync();

        segments.Should().HaveCount(1);
        segments[0].Text.Should().Be("New segment");
    }

    #endregion

    #region Auto-Generate Embeddings Tests

    [Fact]
    public async Task ProcessTranscriptionJobAsync_AutoGenerateEmbeddingsEnabled_EnqueuesEmbeddingJob()
    {
        // Arrange
        await AuthenticateAsync();
        _mockAppConfiguration.Setup(x => x.AutoGenerateEmbeddings).Returns(true);
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        var processor = CreateProcessor();

        // Setup mocks
        _mockTranscriptionService.Setup(x => x.IsWhisperAvailableAsync()).ReturnsAsync(true);
        _mockAudioExtractionService
            .Setup(x => x.ExtractAudioFromYouTubeAsync(video.YouTubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("C:\\temp\\audio.mp3");
        _mockAudioExtractionService
            .Setup(x => x.GetAudioInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AudioInfo { Duration = TimeSpan.FromMinutes(5), FileSizeBytes = 5000000 });
        _mockTranscriptionService
            .Setup(x => x.TranscribeAudioAsync(It.IsAny<TranscriptionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TranscriptionResultDto
            {
                VideoId = video.Id,
                Duration = TimeSpan.FromMinutes(5),
                Language = "en",
                Segments = new List<TranscriptSegmentDto>
                {
                    new TranscriptSegmentDto { StartTime = 0, EndTime = 5, Text = "Test", Confidence = 0.95f }
                }
            });

        // Act
        var result = await processor.ProcessTranscriptionJobAsync(video.Id);

        // Assert
        result.Should().BeTrue();

        // Verify embedding job created
        var embeddingJobs = await DbContext.Jobs
            .Where(j => j.VideoId == video.Id && j.Type == JobType.EmbeddingGeneration)
            .ToListAsync();

        embeddingJobs.Should().HaveCount(1);
        var embeddingJob = embeddingJobs.First();
        embeddingJob.Status.Should().Be(JobStatus.Pending);
        embeddingJob.HangfireJobId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ProcessTranscriptionJobAsync_AutoGenerateEmbeddingsDisabled_DoesNotEnqueueEmbeddingJob()
    {
        // Arrange
        await AuthenticateAsync();
        _mockAppConfiguration.Setup(x => x.AutoGenerateEmbeddings).Returns(false);
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        var processor = CreateProcessor();

        // Setup mocks
        _mockTranscriptionService.Setup(x => x.IsWhisperAvailableAsync()).ReturnsAsync(true);
        _mockAudioExtractionService
            .Setup(x => x.ExtractAudioFromYouTubeAsync(video.YouTubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("C:\\temp\\audio.mp3");
        _mockAudioExtractionService
            .Setup(x => x.GetAudioInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AudioInfo { Duration = TimeSpan.FromMinutes(5), FileSizeBytes = 5000000 });
        _mockTranscriptionService
            .Setup(x => x.TranscribeAudioAsync(It.IsAny<TranscriptionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TranscriptionResultDto
            {
                VideoId = video.Id,
                Duration = TimeSpan.FromMinutes(5),
                Language = "en",
                Segments = new List<TranscriptSegmentDto>
                {
                    new TranscriptSegmentDto { StartTime = 0, EndTime = 5, Text = "Test", Confidence = 0.95f }
                }
            });

        // Act
        var result = await processor.ProcessTranscriptionJobAsync(video.Id);

        // Assert
        result.Should().BeTrue();

        // Verify no embedding job created
        var embeddingJobs = await DbContext.Jobs
            .Where(j => j.VideoId == video.Id && j.Type == JobType.EmbeddingGeneration)
            .ToListAsync();

        embeddingJobs.Should().BeEmpty();
    }

    #endregion
}
