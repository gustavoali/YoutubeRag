using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Application.Interfaces.Services;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;
using YoutubeRag.Infrastructure.Jobs;
using YoutubeRag.Tests.Integration.Helpers;
using YoutubeRag.Tests.Integration.Infrastructure;
using System.Text.Json;
using Hangfire;

namespace YoutubeRag.Tests.Integration.Jobs;

/// <summary>
/// Integration tests for Multi-Stage Pipeline functionality
/// Tests pipeline stage transitions, progress tracking, metadata passing, and error handling
/// </summary>
public class MultiStagePipelineTests : IntegrationTestBase
{
    private readonly IJobRepository _jobRepository;
    private readonly IVideoRepository _videoRepository;
    private readonly ITranscriptSegmentRepository _transcriptSegmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly Mock<IVideoDownloadService> _mockVideoDownloadService;
    private readonly Mock<IAudioExtractionService> _mockAudioExtractionService;
    private readonly Mock<ITranscriptionService> _mockTranscriptionService;
    private readonly Mock<IBackgroundJobClient> _mockBackgroundJobClient;

    public MultiStagePipelineTests(CustomWebApplicationFactory<Program> factory) : base(factory)
    {
        _mockVideoDownloadService = new Mock<IVideoDownloadService>();
        _mockAudioExtractionService = new Mock<IAudioExtractionService>();
        _mockTranscriptionService = new Mock<ITranscriptionService>();
        _mockBackgroundJobClient = new Mock<IBackgroundJobClient>();
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        _jobRepository = Scope.ServiceProvider.GetRequiredService<IJobRepository>();
        _videoRepository = Scope.ServiceProvider.GetRequiredService<IVideoRepository>();
        _transcriptSegmentRepository = Scope.ServiceProvider.GetRequiredService<ITranscriptSegmentRepository>();
        _unitOfWork = Scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Setup default mock behaviors
        _mockVideoDownloadService
            .Setup(x => x.DownloadVideoAsync(It.IsAny<string>(), It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string youtubeId, IProgress<double> progress, CancellationToken ct) =>
            {
                progress?.Report(1.0);
                return $"C:\\temp\\{youtubeId}_video.mp4";
            });

        _mockAudioExtractionService
            .Setup(x => x.ExtractWhisperAudioFromVideoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string videoPath, string videoId, CancellationToken ct) =>
                $"C:\\temp\\{videoId}_whisper.wav");

        _mockBackgroundJobClient
            .Setup(x => x.Enqueue(It.IsAny<System.Linq.Expressions.Expression<Func<object>>>()))
            .Returns("hangfire-job-id");
    }

    #region Pipeline Stage Progression Tests

    [Fact]
    public async Task Pipeline_DownloadStage_CompletesAndEnqueuesAudioExtraction()
    {
        // Arrange
        await AuthenticateAsync();
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        video.Status = VideoStatus.Pending;
        await DbContext.Videos.AddAsync(video);

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = video.Id,
            UserId = video.UserId,
            Type = JobType.Transcription,
            Status = JobStatus.Pending,
            Priority = 1,
            CurrentStage = PipelineStage.None,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Jobs.AddAsync(job);
        await DbContext.SaveChangesAsync();

        var processor = new DownloadJobProcessor(
            _mockVideoDownloadService.Object,
            _videoRepository,
            _jobRepository,
            _unitOfWork,
            _mockBackgroundJobClient.Object,
            Scope.ServiceProvider.GetRequiredService<ILogger<DownloadJobProcessor>>()
        );

        // Act
        await processor.ExecuteAsync(job.Id);

        // Assert
        var updatedJob = await _jobRepository.GetByIdAsync(job.Id);
        updatedJob.Should().NotBeNull();
        updatedJob!.CurrentStage.Should().Be(PipelineStage.Download);
        updatedJob.GetStageProgress()[PipelineStage.Download].Should().Be(100);

        // Verify metadata contains video file path
        updatedJob.Metadata.Should().NotBeNullOrEmpty();
        var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(updatedJob.Metadata!);
        metadata.Should().ContainKey("VideoFilePath");

        // Verify next stage was enqueued
        _mockBackgroundJobClient.Verify(
            x => x.Enqueue(It.IsAny<System.Linq.Expressions.Expression<Action<AudioExtractionJobProcessor>>>()),
            Times.Once);
    }

    [Fact]
    public async Task Pipeline_AudioExtractionStage_ReceivesVideoPathFromMetadata()
    {
        // Arrange
        await AuthenticateAsync();
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);

        var videoFilePath = $"C:\\temp\\{video.YouTubeId}_video.mp4";
        var metadata = new Dictionary<string, object>
        {
            { "VideoFilePath", videoFilePath }
        };

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = video.Id,
            UserId = video.UserId,
            Type = JobType.Transcription,
            Status = JobStatus.Running,
            Priority = 1,
            CurrentStage = PipelineStage.Download,
            Metadata = JsonSerializer.Serialize(metadata),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        job.SetStageProgress(PipelineStage.Download, 100);
        await DbContext.Jobs.AddAsync(job);
        await DbContext.SaveChangesAsync();

        var processor = new AudioExtractionJobProcessor(
            _mockAudioExtractionService.Object,
            _videoRepository,
            _jobRepository,
            _unitOfWork,
            _mockBackgroundJobClient.Object,
            Scope.ServiceProvider.GetRequiredService<ILogger<AudioExtractionJobProcessor>>()
        );

        // Act
        await processor.ExecuteAsync(job.Id);

        // Assert
        var updatedJob = await _jobRepository.GetByIdAsync(job.Id);
        updatedJob.Should().NotBeNull();
        updatedJob!.CurrentStage.Should().Be(PipelineStage.AudioExtraction);
        updatedJob.GetStageProgress()[PipelineStage.AudioExtraction].Should().Be(100);

        // Verify audio extraction was called with correct video path
        _mockAudioExtractionService.Verify(
            x => x.ExtractWhisperAudioFromVideoAsync(videoFilePath, video.Id, It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify metadata now contains audio file path
        var updatedMetadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(updatedJob.Metadata!);
        updatedMetadata.Should().ContainKey("AudioFilePath");
    }

    [Fact]
    public async Task Pipeline_AllStagesComplete_JobStatusCompleted()
    {
        // Test a simplified version where we manually progress through stages
        // (Full integration would require all processors working together)

        // Arrange
        await AuthenticateAsync();
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = video.Id,
            UserId = video.UserId,
            Type = JobType.Transcription,
            Status = JobStatus.Running,
            Priority = 1,
            CurrentStage = PipelineStage.None,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Jobs.AddAsync(job);
        await DbContext.SaveChangesAsync();

        // Act - Simulate progression through all stages
        job.CurrentStage = PipelineStage.Download;
        job.SetStageProgress(PipelineStage.Download, 100);
        job.Progress = job.CalculateOverallProgress();
        await _jobRepository.UpdateAsync(job);
        await _unitOfWork.SaveChangesAsync();

        job.CurrentStage = PipelineStage.AudioExtraction;
        job.SetStageProgress(PipelineStage.AudioExtraction, 100);
        job.Progress = job.CalculateOverallProgress();
        await _jobRepository.UpdateAsync(job);
        await _unitOfWork.SaveChangesAsync();

        job.CurrentStage = PipelineStage.Transcription;
        job.SetStageProgress(PipelineStage.Transcription, 100);
        job.Progress = job.CalculateOverallProgress();
        await _jobRepository.UpdateAsync(job);
        await _unitOfWork.SaveChangesAsync();

        job.CurrentStage = PipelineStage.Segmentation;
        job.SetStageProgress(PipelineStage.Segmentation, 100);
        job.Progress = job.CalculateOverallProgress();
        job.Status = JobStatus.Completed;
        job.CompletedAt = DateTime.UtcNow;
        await _jobRepository.UpdateAsync(job);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        var completedJob = await _jobRepository.GetByIdAsync(job.Id);
        completedJob.Should().NotBeNull();
        completedJob!.Status.Should().Be(JobStatus.Completed);
        completedJob.CurrentStage.Should().Be(PipelineStage.Segmentation);
        completedJob.Progress.Should().Be(100);

        var stageProgress = completedJob.GetStageProgress();
        stageProgress[PipelineStage.Download].Should().Be(100);
        stageProgress[PipelineStage.AudioExtraction].Should().Be(100);
        stageProgress[PipelineStage.Transcription].Should().Be(100);
        stageProgress[PipelineStage.Segmentation].Should().Be(100);
    }

    #endregion

    #region Stage Failure Handling Tests

    [Fact]
    public async Task Pipeline_DownloadStage_Fails_DoesNotEnqueueNextStage()
    {
        // Arrange
        await AuthenticateAsync();
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);

        var job = new Job
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
        await DbContext.Jobs.AddAsync(job);
        await DbContext.SaveChangesAsync();

        // Setup mock to fail
        _mockVideoDownloadService
            .Setup(x => x.DownloadVideoAsync(It.IsAny<string>(), It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network timeout"));

        var processor = new DownloadJobProcessor(
            _mockVideoDownloadService.Object,
            _videoRepository,
            _jobRepository,
            _unitOfWork,
            _mockBackgroundJobClient.Object,
            Scope.ServiceProvider.GetRequiredService<ILogger<DownloadJobProcessor>>()
        );

        // Act
        await Assert.ThrowsAsync<HttpRequestException>(() => processor.ExecuteAsync(job.Id));

        // Assert
        var updatedJob = await _jobRepository.GetByIdAsync(job.Id);
        updatedJob.Should().NotBeNull();
        updatedJob!.Status.Should().Be(JobStatus.Failed);
        updatedJob.CurrentStage.Should().Be(PipelineStage.Download);
        updatedJob.ErrorMessage.Should().Contain("Network timeout");

        // Verify AudioExtraction was NOT enqueued
        _mockBackgroundJobClient.Verify(
            x => x.Enqueue(It.IsAny<System.Linq.Expressions.Expression<Action<AudioExtractionJobProcessor>>>()),
            Times.Never);
    }

    [Fact]
    public async Task Pipeline_TranscriptionStage_Fails_PreviousStagesNotRetried()
    {
        // Arrange
        await AuthenticateAsync();
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);

        var metadata = new Dictionary<string, object>
        {
            { "VideoFilePath", "C:\\temp\\video.mp4" },
            { "AudioFilePath", "C:\\temp\\audio.wav" }
        };

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = video.Id,
            UserId = video.UserId,
            Type = JobType.Transcription,
            Status = JobStatus.Running,
            Priority = 1,
            CurrentStage = PipelineStage.AudioExtraction,
            Metadata = JsonSerializer.Serialize(metadata),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        job.SetStageProgress(PipelineStage.Download, 100);
        job.SetStageProgress(PipelineStage.AudioExtraction, 100);
        await DbContext.Jobs.AddAsync(job);
        await DbContext.SaveChangesAsync();

        // Act - Simulate transcription stage failure
        job.CurrentStage = PipelineStage.Transcription;
        job.Status = JobStatus.Failed;
        job.ErrorMessage = "Whisper model not available";
        job.SetStageProgress(PipelineStage.Transcription, 0);
        job.RetryCount = 1;
        await _jobRepository.UpdateAsync(job);
        await _unitOfWork.SaveChangesAsync();

        // Assert - Previous stages remain completed
        var failedJob = await _jobRepository.GetByIdAsync(job.Id);
        failedJob.Should().NotBeNull();
        failedJob!.Status.Should().Be(JobStatus.Failed);
        failedJob.CurrentStage.Should().Be(PipelineStage.Transcription);

        var stageProgress = failedJob.GetStageProgress();
        stageProgress[PipelineStage.Download].Should().Be(100); // Still complete
        stageProgress[PipelineStage.AudioExtraction].Should().Be(100); // Still complete
        stageProgress[PipelineStage.Transcription].Should().Be(0); // Failed
    }

    #endregion

    #region Progress Tracking Tests

    [Fact]
    public async Task Pipeline_CalculatesWeightedProgress_Download()
    {
        // Arrange
        await AuthenticateAsync();
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = video.Id,
            UserId = video.UserId,
            Type = JobType.Transcription,
            Status = JobStatus.Running,
            Priority = 1,
            CurrentStage = PipelineStage.Download,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        job.SetStageProgress(PipelineStage.Download, 100);
        await DbContext.Jobs.AddAsync(job);
        await DbContext.SaveChangesAsync();

        // Act
        job.Progress = job.CalculateOverallProgress();
        await _jobRepository.UpdateAsync(job);
        await _unitOfWork.SaveChangesAsync();

        // Assert - Download weight = 20%
        var updatedJob = await _jobRepository.GetByIdAsync(job.Id);
        updatedJob!.Progress.Should().Be(20);
    }

    [Theory]
    [InlineData(PipelineStage.Download, 100, 20)]           // Download: 20% weight
    [InlineData(PipelineStage.AudioExtraction, 100, 35)]    // Download (20%) + Audio (15%) = 35%
    [InlineData(PipelineStage.Transcription, 100, 85)]      // Download (20%) + Audio (15%) + Transcription (50%) = 85%
    [InlineData(PipelineStage.Segmentation, 100, 100)]      // All stages complete = 100%
    public async Task Pipeline_CalculatesWeightedProgress_ForEachStage(PipelineStage stage, double stageProgress, int expectedOverallProgress)
    {
        // Arrange
        await AuthenticateAsync();
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = video.Id,
            UserId = video.UserId,
            Type = JobType.Transcription,
            Status = JobStatus.Running,
            Priority = 1,
            CurrentStage = stage,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Set progress for all stages up to and including current stage
        if (stage >= PipelineStage.Download)
            job.SetStageProgress(PipelineStage.Download, 100);
        if (stage >= PipelineStage.AudioExtraction)
            job.SetStageProgress(PipelineStage.AudioExtraction, 100);
        if (stage >= PipelineStage.Transcription)
            job.SetStageProgress(PipelineStage.Transcription, 100);
        if (stage >= PipelineStage.Segmentation)
            job.SetStageProgress(PipelineStage.Segmentation, 100);

        await DbContext.Jobs.AddAsync(job);
        await DbContext.SaveChangesAsync();

        // Act
        job.Progress = job.CalculateOverallProgress();
        await _jobRepository.UpdateAsync(job);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        var updatedJob = await _jobRepository.GetByIdAsync(job.Id);
        updatedJob!.Progress.Should().Be(expectedOverallProgress);
    }

    [Fact]
    public async Task Pipeline_PartialStageProgress_CalculatesCorrectly()
    {
        // Arrange
        await AuthenticateAsync();
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = video.Id,
            UserId = video.UserId,
            Type = JobType.Transcription,
            Status = JobStatus.Running,
            Priority = 1,
            CurrentStage = PipelineStage.Transcription,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Download: 100%, Audio: 100%, Transcription: 50%, Segmentation: 0%
        job.SetStageProgress(PipelineStage.Download, 100);
        job.SetStageProgress(PipelineStage.AudioExtraction, 100);
        job.SetStageProgress(PipelineStage.Transcription, 50);
        job.SetStageProgress(PipelineStage.Segmentation, 0);

        await DbContext.Jobs.AddAsync(job);
        await DbContext.SaveChangesAsync();

        // Act
        job.Progress = job.CalculateOverallProgress();

        // Assert
        // Expected: (20 * 1.0) + (15 * 1.0) + (50 * 0.5) + (15 * 0.0) = 20 + 15 + 25 + 0 = 60%
        job.Progress.Should().Be(60);
    }

    #endregion

    #region Metadata Passing Tests

    [Fact]
    public async Task Pipeline_MetadataPassesBetweenStages()
    {
        // Arrange
        await AuthenticateAsync();
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = video.Id,
            UserId = video.UserId,
            Type = JobType.Transcription,
            Status = JobStatus.Running,
            Priority = 1,
            CurrentStage = PipelineStage.None,
            Metadata = "{}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Jobs.AddAsync(job);
        await DbContext.SaveChangesAsync();

        // Act - Simulate metadata accumulation through stages
        // Stage 1: Download adds video path
        var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(job.Metadata!) ?? new Dictionary<string, object>();
        metadata["VideoFilePath"] = "C:\\temp\\video.mp4";
        job.Metadata = JsonSerializer.Serialize(metadata);
        job.CurrentStage = PipelineStage.Download;
        await _jobRepository.UpdateAsync(job);
        await _unitOfWork.SaveChangesAsync();

        // Stage 2: Audio extraction adds audio path
        metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(job.Metadata!) ?? new Dictionary<string, object>();
        metadata["AudioFilePath"] = "C:\\temp\\audio.wav";
        metadata["AudioDuration"] = "00:05:30";
        metadata["AudioSizeBytes"] = 5242880;
        job.Metadata = JsonSerializer.Serialize(metadata);
        job.CurrentStage = PipelineStage.AudioExtraction;
        await _jobRepository.UpdateAsync(job);
        await _unitOfWork.SaveChangesAsync();

        // Stage 3: Transcription adds transcript metadata
        metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(job.Metadata!) ?? new Dictionary<string, object>();
        metadata["TranscriptionLanguage"] = "en";
        metadata["TranscriptionModel"] = "base";
        job.Metadata = JsonSerializer.Serialize(metadata);
        job.CurrentStage = PipelineStage.Transcription;
        await _jobRepository.UpdateAsync(job);
        await _unitOfWork.SaveChangesAsync();

        // Assert - All metadata preserved
        var finalJob = await _jobRepository.GetByIdAsync(job.Id);
        finalJob.Should().NotBeNull();

        var finalMetadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(finalJob!.Metadata!);
        finalMetadata.Should().ContainKey("VideoFilePath");
        finalMetadata.Should().ContainKey("AudioFilePath");
        finalMetadata.Should().ContainKey("AudioDuration");
        finalMetadata.Should().ContainKey("AudioSizeBytes");
        finalMetadata.Should().ContainKey("TranscriptionLanguage");
        finalMetadata.Should().ContainKey("TranscriptionModel");

        finalMetadata["VideoFilePath"].GetString().Should().Be("C:\\temp\\video.mp4");
        finalMetadata["AudioFilePath"].GetString().Should().Be("C:\\temp\\audio.wav");
        finalMetadata["TranscriptionLanguage"].GetString().Should().Be("en");
    }

    [Fact]
    public async Task Pipeline_MetadataAvailableToAllSubsequentStages()
    {
        // Ensure that once metadata is added, it's available to all subsequent stages

        // Arrange
        await AuthenticateAsync();
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);

        var initialMetadata = new Dictionary<string, object>
        {
            { "VideoFilePath", "C:\\temp\\video.mp4" },
            { "VideoFormat", "mp4" },
            { "VideoResolution", "1920x1080" }
        };

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = video.Id,
            UserId = video.UserId,
            Type = JobType.Transcription,
            Status = JobStatus.Running,
            Priority = 1,
            CurrentStage = PipelineStage.Download,
            Metadata = JsonSerializer.Serialize(initialMetadata),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Jobs.AddAsync(job);
        await DbContext.SaveChangesAsync();

        // Act - AudioExtraction stage should access VideoFilePath
        var retrievedJob = await _jobRepository.GetByIdAsync(job.Id);
        var metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(retrievedJob!.Metadata!);

        // Assert
        metadata.Should().ContainKey("VideoFilePath");
        metadata.Should().ContainKey("VideoFormat");
        metadata.Should().ContainKey("VideoResolution");

        var videoFilePath = metadata["VideoFilePath"].GetString();
        videoFilePath.Should().Be("C:\\temp\\video.mp4");
    }

    #endregion

    #region Stage Independence Tests

    [Fact]
    public async Task Pipeline_EachStageTrackedIndependently()
    {
        // Verify that each stage maintains its own progress independently

        // Arrange
        await AuthenticateAsync();
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = video.Id,
            UserId = video.UserId,
            Type = JobType.Transcription,
            Status = JobStatus.Running,
            Priority = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Set different progress for different stages
        job.SetStageProgress(PipelineStage.Download, 100);
        job.SetStageProgress(PipelineStage.AudioExtraction, 75);
        job.SetStageProgress(PipelineStage.Transcription, 30);
        job.SetStageProgress(PipelineStage.Segmentation, 0);

        await DbContext.Jobs.AddAsync(job);
        await DbContext.SaveChangesAsync();

        // Act
        var retrievedJob = await _jobRepository.GetByIdAsync(job.Id);
        var stageProgress = retrievedJob!.GetStageProgress();

        // Assert - Each stage maintains independent progress
        stageProgress[PipelineStage.Download].Should().Be(100);
        stageProgress[PipelineStage.AudioExtraction].Should().Be(75);
        stageProgress[PipelineStage.Transcription].Should().Be(30);
        stageProgress[PipelineStage.Segmentation].Should().Be(0);
    }

    [Fact]
    public async Task Pipeline_StageProgressSerialization_WorksCorrectly()
    {
        // Test that stage progress JSON serialization/deserialization works correctly

        // Arrange
        await AuthenticateAsync();
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = video.Id,
            UserId = video.UserId,
            Type = JobType.Transcription,
            Status = JobStatus.Running,
            Priority = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act - Set stage progress and save
        job.SetStageProgress(PipelineStage.Download, 100);
        job.SetStageProgress(PipelineStage.AudioExtraction, 50);
        await DbContext.Jobs.AddAsync(job);
        await DbContext.SaveChangesAsync();

        // Retrieve in a new context to ensure serialization worked
        var retrievedJob = await _jobRepository.GetByIdAsync(job.Id);
        var stageProgress = retrievedJob!.GetStageProgress();

        // Assert
        retrievedJob.StageProgressJson.Should().NotBeNullOrEmpty();
        stageProgress.Should().ContainKey(PipelineStage.Download);
        stageProgress.Should().ContainKey(PipelineStage.AudioExtraction);
        stageProgress[PipelineStage.Download].Should().Be(100);
        stageProgress[PipelineStage.AudioExtraction].Should().Be(50);
    }

    #endregion
}
