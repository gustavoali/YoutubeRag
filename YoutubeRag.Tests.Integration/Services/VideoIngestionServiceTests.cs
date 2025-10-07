using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using YoutubeRag.Application.Configuration;
using YoutubeRag.Application.DTOs.Video;
using YoutubeRag.Application.Exceptions;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Application.Interfaces.Services;
using YoutubeRag.Application.Services;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;
using YoutubeRag.Tests.Integration.Infrastructure;

namespace YoutubeRag.Tests.Integration.Services;

/// <summary>
/// Integration tests for VideoIngestionService
/// Tests URL validation, duplicate detection, user creation, FK constraints, and transaction rollback
/// </summary>
public class VideoIngestionServiceTests : IntegrationTestBase
{
    private readonly Mock<IYouTubeService> _mockYouTubeService;
    private readonly Mock<IMetadataExtractionService> _mockMetadataService;
    private readonly Mock<IAppConfiguration> _mockAppConfiguration;

    public VideoIngestionServiceTests(CustomWebApplicationFactory<Program> factory) : base(factory)
    {
        _mockYouTubeService = new Mock<IYouTubeService>();
        _mockMetadataService = new Mock<IMetadataExtractionService>();
        _mockAppConfiguration = new Mock<IAppConfiguration>();

        // Default mock configuration
        _mockAppConfiguration.Setup(x => x.Environment).Returns("Local");
        _mockAppConfiguration.Setup(x => x.AutoTranscribe).Returns(false); // Disable for simpler tests
    }

    private VideoIngestionService CreateService()
    {
        return new VideoIngestionService(
            Scope.ServiceProvider.GetRequiredService<IVideoRepository>(),
            Scope.ServiceProvider.GetRequiredService<IJobRepository>(),
            Scope.ServiceProvider.GetRequiredService<IUserRepository>(),
            Scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
            _mockYouTubeService.Object,
            _mockMetadataService.Object,
            _mockAppConfiguration.Object,
            Scope.ServiceProvider.GetRequiredService<IBackgroundJobService>(),
            Scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<VideoIngestionService>>()
        );
    }

    #region URL Validation Tests

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/v/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    public async Task ValidateYouTubeUrlAsync_WithValidUrls_ExtractsCorrectYouTubeId(string url, string expectedId)
    {
        // Arrange
        var service = CreateService();
        _mockMetadataService
            .Setup(x => x.IsVideoAccessibleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var (isValid, youTubeId, errorMessage) = await service.ValidateYouTubeUrlAsync(url);

        // Assert
        isValid.Should().BeTrue();
        youTubeId.Should().Be(expectedId);
        errorMessage.Should().BeNull();
    }

    [Theory]
    [InlineData("https://www.google.com")]
    [InlineData("https://vimeo.com/12345")]
    [InlineData("not-a-url")]
    [InlineData("https://www.youtube.com/")]
    public async Task ValidateYouTubeUrlAsync_WithInvalidUrls_ReturnsValidationError(string invalidUrl)
    {
        // Arrange
        var service = CreateService();

        // Act
        var (isValid, youTubeId, errorMessage) = await service.ValidateYouTubeUrlAsync(invalidUrl);

        // Assert
        isValid.Should().BeFalse();
        youTubeId.Should().BeNull();
        errorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ValidateYouTubeUrlAsync_WithEmptyUrl_ReturnsValidationError()
    {
        // Arrange
        var service = CreateService();

        // Act
        var (isValid, youTubeId, errorMessage) = await service.ValidateYouTubeUrlAsync("");

        // Assert
        isValid.Should().BeFalse();
        youTubeId.Should().BeNull();
        errorMessage.Should().Be("URL cannot be empty");
    }

    [Fact]
    public void ExtractYouTubeId_WithValidUrls_ExtractsCorrectId()
    {
        // Arrange
        var service = CreateService();
        var testCases = new[]
        {
            ("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ"),
            ("https://youtu.be/jNQXAC9IVRw", "jNQXAC9IVRw"),
            ("https://www.youtube.com/embed/abc123_-ABC", "abc123_-ABC"),
        };

        foreach (var (url, expectedId) in testCases)
        {
            // Act
            var extractedId = service.ExtractYouTubeId(url);

            // Assert
            extractedId.Should().Be(expectedId, $"URL {url} should extract to {expectedId}");
        }
    }

    #endregion

    #region Duplicate Detection Tests

    [Fact]
    public async Task IngestVideoFromUrlAsync_FirstIngestion_SuccessfullyCreatesVideo()
    {
        // Arrange
        await AuthenticateAsync();
        var service = CreateService();
        var youTubeId = "dQw4w9WgXcQ";
        var url = $"https://www.youtube.com/watch?v={youTubeId}";

        // Setup mocks
        _mockMetadataService
            .Setup(x => x.IsVideoAccessibleAsync(youTubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockMetadataService
            .Setup(x => x.ExtractMetadataAsync(youTubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VideoMetadataDto
            {
                Title = "Test Video",
                Description = "Test Description",
                Duration = TimeSpan.FromMinutes(5),
                ViewCount = 1000
            });

        var request = new VideoIngestionRequestDto(
            Url: url,
            UserId: AuthenticatedUserId!,
            Priority: ProcessingPriority.Normal
        );

        // Act
        var response = await service.IngestVideoFromUrlAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.VideoId.Should().NotBeNullOrEmpty();
        response.YouTubeId.Should().Be(youTubeId);
        response.Status.Should().Be(VideoStatus.Pending.ToString());

        // Verify video in database
        var video = await DbContext.Videos.FirstOrDefaultAsync(v => v.YouTubeId == youTubeId);
        video.Should().NotBeNull();
        video!.Title.Should().Be("Test Video");
        video.UserId.Should().Be(AuthenticatedUserId);
    }

    [Fact]
    public async Task IngestVideoFromUrlAsync_DuplicateYouTubeId_ThrowsDuplicateResourceException()
    {
        // Arrange
        await AuthenticateAsync();
        var service = CreateService();
        var youTubeId = "dQw4w9WgXcQ";
        var url = $"https://www.youtube.com/watch?v={youTubeId}";

        // Create existing video in database
        var existingVideo = new Video
        {
            Id = Guid.NewGuid().ToString(),
            YouTubeId = youTubeId,
            Title = "Existing Video",
            UserId = AuthenticatedUserId!,
            Status = VideoStatus.Pending,
            Url = url,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Videos.AddAsync(existingVideo);
        await DbContext.SaveChangesAsync();

        // Setup mocks
        _mockMetadataService
            .Setup(x => x.IsVideoAccessibleAsync(youTubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new VideoIngestionRequestDto(
            Url: url,
            UserId: AuthenticatedUserId!,
            Priority: ProcessingPriority.Normal
        );

        // Act & Assert
        await Assert.ThrowsAsync<DuplicateResourceException>(
            async () => await service.IngestVideoFromUrlAsync(request)
        );

        // Verify only one video exists
        var videoCount = await DbContext.Videos.CountAsync(v => v.YouTubeId == youTubeId);
        videoCount.Should().Be(1);
    }

    [Fact]
    public async Task IsVideoAlreadyIngestedAsync_ExistingVideo_ReturnsTrue()
    {
        // Arrange
        await AuthenticateAsync();
        var service = CreateService();
        var youTubeId = "dQw4w9WgXcQ";

        var existingVideo = new Video
        {
            Id = Guid.NewGuid().ToString(),
            YouTubeId = youTubeId,
            Title = "Existing Video",
            UserId = AuthenticatedUserId!,
            Status = VideoStatus.Pending,
            Url = $"https://www.youtube.com/watch?v={youTubeId}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Videos.AddAsync(existingVideo);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await service.IsVideoAlreadyIngestedAsync(youTubeId);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region User Creation in Local Mode Tests

    [Fact]
    public async Task IngestVideoFromUrlAsync_LocalEnvironment_NonExistentUser_AutoCreatesUser()
    {
        // Arrange
        _mockAppConfiguration.Setup(x => x.Environment).Returns("Local");
        var service = CreateService();
        var youTubeId = "dQw4w9WgXcQ";
        var url = $"https://www.youtube.com/watch?v={youTubeId}";
        var testUserId = "test-user-integration-001";

        // Setup mocks
        _mockMetadataService
            .Setup(x => x.IsVideoAccessibleAsync(youTubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockMetadataService
            .Setup(x => x.ExtractMetadataAsync(youTubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VideoMetadataDto
            {
                Title = "Test Video",
                Description = "Test Description",
                Duration = TimeSpan.FromMinutes(5)
            });

        var request = new VideoIngestionRequestDto(
            Url: url,
            UserId: testUserId,
            Priority: ProcessingPriority.Normal
        );

        // Act
        var response = await service.IngestVideoFromUrlAsync(request);

        // Assert
        response.Should().NotBeNull();

        // Verify user was auto-created
        var user = await DbContext.Users.FirstOrDefaultAsync(u => u.Id == testUserId);
        user.Should().NotBeNull();
        user!.Name.Should().Be("Test User");
        user.Email.Should().Be("test@example.com");

        // Verify video is associated with created user
        var video = await DbContext.Videos.FirstOrDefaultAsync(v => v.YouTubeId == youTubeId);
        video.Should().NotBeNull();
        video!.UserId.Should().Be(testUserId);
    }

    [Fact]
    public async Task IngestVideoFromUrlAsync_DevelopmentEnvironment_NonExistentUser_AutoCreatesUser()
    {
        // Arrange
        _mockAppConfiguration.Setup(x => x.Environment).Returns("Development");
        var service = CreateService();
        var youTubeId = "jNQXAC9IVRw";
        var url = $"https://www.youtube.com/watch?v={youTubeId}";
        var testUserId = "test-user-dev-002";

        // Setup mocks
        _mockMetadataService
            .Setup(x => x.IsVideoAccessibleAsync(youTubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockMetadataService
            .Setup(x => x.ExtractMetadataAsync(youTubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VideoMetadataDto { Title = "Dev Test Video" });

        var request = new VideoIngestionRequestDto(
            Url: url,
            UserId: testUserId,
            Priority: ProcessingPriority.Normal
        );

        // Act
        var response = await service.IngestVideoFromUrlAsync(request);

        // Assert
        response.Should().NotBeNull();

        // Verify user was auto-created
        var user = await DbContext.Users.FirstOrDefaultAsync(u => u.Id == testUserId);
        user.Should().NotBeNull();
    }

    #endregion

    #region FK Constraint Enforcement in Production Mode Tests

    [Fact]
    public async Task IngestVideoFromUrlAsync_ProductionEnvironment_NonExistentUser_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockAppConfiguration.Setup(x => x.Environment).Returns("Production");
        var service = CreateService();
        var youTubeId = "dQw4w9WgXcQ";
        var url = $"https://www.youtube.com/watch?v={youTubeId}";
        var nonExistentUserId = "non-existent-user-id";

        // Setup mocks
        _mockMetadataService
            .Setup(x => x.IsVideoAccessibleAsync(youTubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockMetadataService
            .Setup(x => x.ExtractMetadataAsync(youTubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VideoMetadataDto { Title = "Production Test Video" });

        var request = new VideoIngestionRequestDto(
            Url: url,
            UserId: nonExistentUserId,
            Priority: ProcessingPriority.Normal
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.IngestVideoFromUrlAsync(request)
        );

        exception.Message.Should().Contain(nonExistentUserId);

        // Verify no video was created
        var videoCount = await DbContext.Videos.CountAsync(v => v.YouTubeId == youTubeId);
        videoCount.Should().Be(0);

        // Verify no job was created
        var jobCount = await DbContext.Jobs.CountAsync();
        jobCount.Should().Be(0);
    }

    #endregion

    #region Transaction Rollback Tests

    [Fact]
    public async Task IngestVideoFromUrlAsync_InvalidYouTubeUrl_ThrowsBusinessValidationException_NoDataSaved()
    {
        // Arrange
        await AuthenticateAsync();
        var service = CreateService();
        var invalidUrl = "https://www.google.com";

        var request = new VideoIngestionRequestDto(
            Url: invalidUrl,
            UserId: AuthenticatedUserId!,
            Priority: ProcessingPriority.Normal
        );

        // Act & Assert
        await Assert.ThrowsAsync<BusinessValidationException>(
            async () => await service.IngestVideoFromUrlAsync(request)
        );

        // Verify no video was created
        var videoCount = await DbContext.Videos.CountAsync();
        videoCount.Should().Be(0);

        // Verify no job was created
        var jobCount = await DbContext.Jobs.CountAsync();
        jobCount.Should().Be(0);
    }

    [Fact]
    public async Task IngestVideoFromUrlAsync_MetadataExtractionFails_ContinuesWithBasicInfo()
    {
        // Arrange
        await AuthenticateAsync();
        var service = CreateService();
        var youTubeId = "dQw4w9WgXcQ";
        var url = $"https://www.youtube.com/watch?v={youTubeId}";

        // Setup mocks - metadata extraction fails
        _mockMetadataService
            .Setup(x => x.IsVideoAccessibleAsync(youTubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockMetadataService
            .Setup(x => x.ExtractMetadataAsync(youTubeId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Metadata extraction failed"));

        var request = new VideoIngestionRequestDto(
            Url: url,
            UserId: AuthenticatedUserId!,
            Title: "Manual Title",
            Priority: ProcessingPriority.Normal
        );

        // Act
        var response = await service.IngestVideoFromUrlAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.VideoId.Should().NotBeNullOrEmpty();

        // Verify video was created with manual title
        var video = await DbContext.Videos.FirstOrDefaultAsync(v => v.YouTubeId == youTubeId);
        video.Should().NotBeNull();
        video!.Title.Should().Be("Manual Title");
    }

    #endregion

    #region Auto-Transcribe Job Creation Tests

    [Fact]
    public async Task IngestVideoFromUrlAsync_AutoTranscribeEnabled_CreatesTranscriptionJob()
    {
        // Arrange
        await AuthenticateAsync();
        _mockAppConfiguration.Setup(x => x.AutoTranscribe).Returns(true);
        var service = CreateService();
        var youTubeId = "dQw4w9WgXcQ";
        var url = $"https://www.youtube.com/watch?v={youTubeId}";

        // Setup mocks
        _mockMetadataService
            .Setup(x => x.IsVideoAccessibleAsync(youTubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockMetadataService
            .Setup(x => x.ExtractMetadataAsync(youTubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VideoMetadataDto
            {
                Title = "Test Video for Transcription",
                Duration = TimeSpan.FromMinutes(5)
            });

        var request = new VideoIngestionRequestDto(
            Url: url,
            UserId: AuthenticatedUserId!,
            Priority: ProcessingPriority.High
        );

        // Act
        var response = await service.IngestVideoFromUrlAsync(request);

        // Assert
        response.Should().NotBeNull();

        // Verify video was created
        var video = await DbContext.Videos
            .Include(v => v.Jobs)
            .FirstOrDefaultAsync(v => v.YouTubeId == youTubeId);

        video.Should().NotBeNull();
        video!.TranscriptionStatus.Should().Be(TranscriptionStatus.Pending);

        // Verify transcription job was created
        var transcriptionJobs = await DbContext.Jobs
            .Where(j => j.VideoId == video.Id && j.Type == JobType.Transcription)
            .ToListAsync();

        transcriptionJobs.Should().HaveCount(1);
        var transcriptionJob = transcriptionJobs.First();
        transcriptionJob.Status.Should().Be(JobStatus.Pending);
        transcriptionJob.Priority.Should().Be((int)ProcessingPriority.High);
        transcriptionJob.HangfireJobId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task IngestVideoFromUrlAsync_AutoTranscribeDisabled_DoesNotCreateTranscriptionJob()
    {
        // Arrange
        await AuthenticateAsync();
        _mockAppConfiguration.Setup(x => x.AutoTranscribe).Returns(false);
        var service = CreateService();
        var youTubeId = "jNQXAC9IVRw";
        var url = $"https://www.youtube.com/watch?v={youTubeId}";

        // Setup mocks
        _mockMetadataService
            .Setup(x => x.IsVideoAccessibleAsync(youTubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockMetadataService
            .Setup(x => x.ExtractMetadataAsync(youTubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VideoMetadataDto { Title = "Test Video No Transcription" });

        var request = new VideoIngestionRequestDto(
            Url: url,
            UserId: AuthenticatedUserId!,
            Priority: ProcessingPriority.Normal
        );

        // Act
        var response = await service.IngestVideoFromUrlAsync(request);

        // Assert
        response.Should().NotBeNull();

        // Verify no transcription job was created
        var video = await DbContext.Videos.FirstOrDefaultAsync(v => v.YouTubeId == youTubeId);
        var transcriptionJobs = await DbContext.Jobs
            .Where(j => j.VideoId == video!.Id && j.Type == JobType.Transcription)
            .ToListAsync();

        transcriptionJobs.Should().BeEmpty();
    }

    #endregion
}
