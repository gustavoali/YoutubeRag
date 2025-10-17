using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using YoutubeRag.Application.DTOs.Video;
using YoutubeRag.Application.Exceptions;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Application.Services;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;
using YoutubeRag.Tests.Unit.Builders.Entities;
using YoutubeRag.Tests.Unit.Builders.VideoDtos;

namespace YoutubeRag.Tests.Unit.Application.Services;

public class VideoServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<VideoService>> _mockLogger;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly Mock<IVideoRepository> _mockVideoRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ITranscriptSegmentRepository> _mockTranscriptRepository;
    private readonly Mock<IJobRepository> _mockJobRepository;
    private readonly VideoService _videoService;

    public VideoServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<VideoService>>();
        _mockCache = new Mock<IMemoryCache>();
        _mockVideoRepository = new Mock<IVideoRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockTranscriptRepository = new Mock<ITranscriptSegmentRepository>();
        _mockJobRepository = new Mock<IJobRepository>();

        // Setup UnitOfWork to return mock repositories
        _mockUnitOfWork.Setup(u => u.Videos).Returns(_mockVideoRepository.Object);
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
        _mockUnitOfWork.Setup(u => u.TranscriptSegments).Returns(_mockTranscriptRepository.Object);
        _mockUnitOfWork.Setup(u => u.Jobs).Returns(_mockJobRepository.Object);

        // Setup mock cache for rate limiting (returns 0 count by default)
        var mockCacheEntry = new Mock<ICacheEntry>();
        _mockCache
            .Setup(c => c.CreateEntry(It.IsAny<object>()))
            .Returns(mockCacheEntry.Object);

        object cacheValue = 0;
        _mockCache
            .Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(false);

        _videoService = new VideoService(
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockLogger.Object,
            _mockCache.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingVideo_ReturnsVideoDto()
    {
        // Arrange
        var videoId = "video-123";
        var video = VideoBuilder.CreateValid();
        var videoDto = new VideoDto { Id = videoId, Title = "Test Video" };

        _mockVideoRepository
            .Setup(r => r.GetByIdAsync(videoId))
            .ReturnsAsync(video);

        _mockMapper
            .Setup(m => m.Map<VideoDto>(video))
            .Returns(videoDto);

        // Act
        var result = await _videoService.GetByIdAsync(videoId);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(videoDto);
        _mockVideoRepository.Verify(r => r.GetByIdAsync(videoId), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentVideo_ReturnsNull()
    {
        // Arrange
        var videoId = "non-existent";

        _mockVideoRepository
            .Setup(r => r.GetByIdAsync(videoId))
            .ReturnsAsync((Video?)null);

        // Act
        var result = await _videoService.GetByIdAsync(videoId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WithoutUserId_ReturnsPaginatedVideos()
    {
        // Arrange
        var videos = new List<Video>
        {
            VideoBuilder.CreateValid(),
            VideoBuilder.CreateValid(),
            VideoBuilder.CreateValid()
        };

        var videoListDtos = new List<VideoListDto>
        {
            new() { Id = "1", Title = "Video 1" },
            new() { Id = "2", Title = "Video 2" }
        };

        _mockVideoRepository
            .Setup(r => r.CountAsync(null))
            .ReturnsAsync(3);

        _mockVideoRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(videos);

        _mockMapper
            .Setup(m => m.Map<List<VideoListDto>>(It.IsAny<IEnumerable<Video>>()))
            .Returns(videoListDtos);

        // Act
        var result = await _videoService.GetAllAsync(page: 1, pageSize: 2);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(3);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task GetAllAsync_WithUserId_ReturnsPaginatedUserVideos()
    {
        // Arrange
        var userId = "user-123";
        var videos = new List<Video>
        {
            VideoBuilder.CreateValid(),
            VideoBuilder.CreateValid()
        };

        var videoListDtos = new List<VideoListDto>
        {
            new() { Id = "1", Title = "Video 1" },
            new() { Id = "2", Title = "Video 2" }
        };

        _mockVideoRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Video, bool>>>()))
            .ReturnsAsync(videos);

        _mockMapper
            .Setup(m => m.Map<List<VideoListDto>>(It.IsAny<IEnumerable<Video>>()))
            .Returns(videoListDtos);

        // Act
        var result = await _videoService.GetAllAsync(page: 1, pageSize: 10, userId: userId);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesVideoSuccessfully()
    {
        // Arrange
        var userId = "user-123";
        var createDto = CreateVideoDtoBuilder.CreateValid();
        var user = UserBuilder.CreateValid();
        var video = VideoBuilder.CreateValid();
        var videoDto = new VideoDto { Id = video.Id, Title = createDto.Title };

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _mockMapper
            .Setup(m => m.Map<Video>(createDto))
            .Returns(video);

        _mockMapper
            .Setup(m => m.Map<VideoDto>(It.IsAny<Video>()))
            .Returns(videoDto);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _videoService.CreateAsync(createDto, userId);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be(createDto.Title);
        _mockVideoRepository.Verify(r => r.AddAsync(It.IsAny<Video>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentUser_ThrowsEntityNotFoundException()
    {
        // Arrange
        var userId = "non-existent-user";
        var createDto = CreateVideoDtoBuilder.CreateValid();

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _videoService.CreateAsync(createDto, userId);

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage("*User*not found*");
    }

    [Fact]
    public async Task UpdateAsync_WithValidDto_UpdatesVideoSuccessfully()
    {
        // Arrange
        var videoId = "video-123";
        var updateDto = UpdateVideoDtoBuilder.CreateWithTitleUpdate();
        var video = VideoBuilder.CreateValid();
        var videoDto = new VideoDto { Id = videoId, Title = "Updated Title" };

        _mockVideoRepository
            .Setup(r => r.GetByIdAsync(videoId))
            .ReturnsAsync(video);

        _mockMapper
            .Setup(m => m.Map<VideoDto>(It.IsAny<Video>()))
            .Returns(videoDto);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _videoService.UpdateAsync(videoId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Updated Title");
        _mockVideoRepository.Verify(r => r.UpdateAsync(It.IsAny<Video>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentVideo_ThrowsEntityNotFoundException()
    {
        // Arrange
        var videoId = "non-existent";
        var updateDto = UpdateVideoDtoBuilder.CreateWithTitleUpdate();

        _mockVideoRepository
            .Setup(r => r.GetByIdAsync(videoId))
            .ReturnsAsync((Video?)null);

        // Act
        var act = async () => await _videoService.UpdateAsync(videoId, updateDto);

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage("*Video*not found*");
    }

    [Fact]
    public async Task UpdateAsync_WithPartialUpdate_OnlyUpdatesProvidedFields()
    {
        // Arrange
        var videoId = "video-123";
        var video = VideoBuilder.CreateValid();
        var originalTitle = video.Title;
        var updateDto = new UpdateVideoDtoBuilder()
            .WithDescription("New Description")
            .Build();

        _mockVideoRepository
            .Setup(r => r.GetByIdAsync(videoId))
            .ReturnsAsync(video);

        _mockMapper
            .Setup(m => m.Map<VideoDto>(It.IsAny<Video>()))
            .Returns(new VideoDto { Id = videoId });

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        await _videoService.UpdateAsync(videoId, updateDto);

        // Assert
        video.Title.Should().Be(originalTitle); // Title should not change
        video.Description.Should().Be("New Description"); // Description should change
    }

    [Fact]
    public async Task DeleteAsync_WithExistingVideo_DeletesSuccessfully()
    {
        // Arrange
        var videoId = "video-123";
        var video = VideoBuilder.CreateValid();

        _mockVideoRepository
            .Setup(r => r.GetByIdAsync(videoId))
            .ReturnsAsync(video);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        await _videoService.DeleteAsync(videoId);

        // Assert
        _mockVideoRepository.Verify(r => r.DeleteAsync(videoId), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentVideo_ThrowsEntityNotFoundException()
    {
        // Arrange
        var videoId = "non-existent";

        _mockVideoRepository
            .Setup(r => r.GetByIdAsync(videoId))
            .ReturnsAsync((Video?)null);

        // Act
        var act = async () => await _videoService.DeleteAsync(videoId);

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage("*Video*not found*");
    }

    [Fact]
    public async Task GetDetailsAsync_WithExistingVideo_ReturnsVideoDetails()
    {
        // Arrange
        var videoId = "video-123";
        var video = VideoBuilder.CreateValid();
        var videoDetailsDto = new VideoDetailsDto
        {
            Id = videoId,
            Title = "Test Video"
        };

        _mockVideoRepository
            .Setup(r => r.GetByIdAsync(videoId))
            .ReturnsAsync(video);

        _mockMapper
            .Setup(m => m.Map<VideoDetailsDto>(video))
            .Returns(videoDetailsDto);

        // Act
        var result = await _videoService.GetDetailsAsync(videoId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(videoId);
        result.Title.Should().Be("Test Video");
    }

    [Fact]
    public async Task GetDetailsAsync_WithNonExistentVideo_ThrowsEntityNotFoundException()
    {
        // Arrange
        var videoId = "non-existent";

        _mockVideoRepository
            .Setup(r => r.GetByIdAsync(videoId))
            .ReturnsAsync((Video?)null);

        // Act
        var act = async () => await _videoService.GetDetailsAsync(videoId);

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage("*Video*not found*");
    }

    [Fact]
    public async Task GetStatsAsync_WithExistingVideo_ReturnsStats()
    {
        // Arrange
        var videoId = "video-123";
        var video = VideoBuilder.CreateValid();
        var transcripts = new List<TranscriptSegment>
        {
            new() { VideoId = videoId, Confidence = 0.95 },
            new() { VideoId = videoId, Confidence = 0.85 }
        };
        var jobs = new List<Job>
        {
            new() { VideoId = videoId, Status = JobStatus.Completed },
            new() { VideoId = videoId, Status = JobStatus.Failed }
        };

        _mockVideoRepository
            .Setup(r => r.GetByIdAsync(videoId))
            .ReturnsAsync(video);

        _mockTranscriptRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TranscriptSegment, bool>>>(), default))
            .ReturnsAsync(transcripts);

        _mockJobRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Job, bool>>>()))
            .ReturnsAsync(jobs);

        // Act
        var result = await _videoService.GetStatsAsync(videoId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(videoId);
        result.TotalTranscriptSegments.Should().Be(2);
        result.TotalJobs.Should().Be(2);
        result.CompletedJobs.Should().Be(1);
        result.FailedJobs.Should().Be(1);
        result.AverageConfidence.Should().BeApproximately(0.90, 0.0001); // (0.95 + 0.85) / 2
    }

    [Fact]
    public async Task GetStatsAsync_WithNonExistentVideo_ThrowsEntityNotFoundException()
    {
        // Arrange
        var videoId = "non-existent";

        _mockVideoRepository
            .Setup(r => r.GetByIdAsync(videoId))
            .ReturnsAsync((Video?)null);

        // Act
        var act = async () => await _videoService.GetStatsAsync(videoId);

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage("*Video*not found*");
    }

    [Fact]
    public async Task GetByUserIdAsync_WithVideos_ReturnsVideoList()
    {
        // Arrange
        var userId = "user-123";
        var videos = new List<Video>
        {
            VideoBuilder.CreateValid(),
            VideoBuilder.CreateValid()
        };
        var videoListDtos = new List<VideoListDto>
        {
            new() { Id = "1", Title = "Video 1" },
            new() { Id = "2", Title = "Video 2" }
        };

        _mockVideoRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Video, bool>>>()))
            .ReturnsAsync(videos);

        _mockMapper
            .Setup(m => m.Map<List<VideoListDto>>(videos))
            .Returns(videoListDtos);

        // Act
        var result = await _videoService.GetByUserIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Title.Should().Be("Video 1");
    }

    [Fact]
    public async Task GetByUserIdAsync_WithNoVideos_ReturnsEmptyList()
    {
        // Arrange
        var userId = "user-123";
        var emptyList = new List<Video>();

        _mockVideoRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Video, bool>>>()))
            .ReturnsAsync(emptyList);

        _mockMapper
            .Setup(m => m.Map<List<VideoListDto>>(emptyList))
            .Returns(new List<VideoListDto>());

        // Act
        var result = await _videoService.GetByUserIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #region SubmitVideoFromUrlAsync Tests (US-101)

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/v/dQw4w9WgXcQ")]
    public async Task SubmitVideoFromUrlAsync_WithValidYouTubeUrl_ExtractsCorrectVideoId(string url)
    {
        // Arrange
        var userId = "user-123";
        var submitDto = new SubmitVideoDto { Url = url };
        var expectedYoutubeId = "dQw4w9WgXcQ";

        _mockVideoRepository
            .Setup(r => r.GetByYouTubeIdAsync(expectedYoutubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Video?)null);

        // Setup transaction to capture and execute the action
        _mockUnitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<VideoSubmissionResultDto>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<VideoSubmissionResultDto>>, CancellationToken>((func, ct) => func());

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _videoService.SubmitVideoFromUrlAsync(submitDto, userId);

        // Assert
        result.Should().NotBeNull();
        result.YouTubeId.Should().Be(expectedYoutubeId);
        result.IsExisting.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public async Task SubmitVideoFromUrlAsync_WithEmptyUrl_ThrowsArgumentException(string invalidUrl)
    {
        // Arrange
        var userId = "user-123";
        var submitDto = new SubmitVideoDto { Url = invalidUrl! };

        // Act
        var act = async () => await _videoService.SubmitVideoFromUrlAsync(submitDto, userId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*URL cannot be empty*");
    }

    [Theory]
    [InlineData("https://vimeo.com/123456")]
    [InlineData("https://www.google.com")]
    [InlineData("not-a-url")]
    [InlineData("https://youtube.com/invalid")]
    public async Task SubmitVideoFromUrlAsync_WithNonYouTubeUrl_ThrowsArgumentException(string invalidUrl)
    {
        // Arrange
        var userId = "user-123";
        var submitDto = new SubmitVideoDto { Url = invalidUrl };

        // Act
        var act = async () => await _videoService.SubmitVideoFromUrlAsync(submitDto, userId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Invalid YouTube URL format*");
    }

    [Fact]
    public async Task SubmitVideoFromUrlAsync_WithDuplicateVideo_ReturnsExistingVideo()
    {
        // Arrange
        var userId = "user-123";
        var youtubeId = "dQw4w9WgXcQ";
        var submitDto = new SubmitVideoDto { Url = $"https://www.youtube.com/watch?v={youtubeId}" };

        var existingVideo = VideoBuilder.CreateValid();
        existingVideo.YouTubeId = youtubeId;
        existingVideo.Title = "Existing Video";

        var existingJob = new Job
        {
            Id = "job-123",
            VideoId = existingVideo.Id,
            Status = JobStatus.Completed
        };

        _mockVideoRepository
            .Setup(r => r.GetByYouTubeIdAsync(youtubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingVideo);

        _mockJobRepository
            .Setup(r => r.GetLatestByVideoIdAsync(existingVideo.Id))
            .ReturnsAsync(existingJob);

        // Act
        var result = await _videoService.SubmitVideoFromUrlAsync(submitDto, userId);

        // Assert
        result.Should().NotBeNull();
        result.VideoId.Should().Be(existingVideo.Id);
        result.JobId.Should().Be(existingJob.Id);
        result.Title.Should().Be("Existing Video");
        result.IsExisting.Should().BeTrue();
        result.Message.Should().Contain("already processed");

        // Verify no new video or job was created
        _mockVideoRepository.Verify(r => r.AddAsync(It.IsAny<Video>()), Times.Never);
        _mockJobRepository.Verify(r => r.AddAsync(It.IsAny<Job>()), Times.Never);
    }

    [Fact]
    public async Task SubmitVideoFromUrlAsync_WithDuplicateVideoAndNoJob_ReturnsExistingVideoWithEmptyJobId()
    {
        // Arrange
        var userId = "user-123";
        var youtubeId = "dQw4w9WgXcQ";
        var submitDto = new SubmitVideoDto { Url = $"https://www.youtube.com/watch?v={youtubeId}" };

        var existingVideo = VideoBuilder.CreateValid();
        existingVideo.YouTubeId = youtubeId;

        _mockVideoRepository
            .Setup(r => r.GetByYouTubeIdAsync(youtubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingVideo);

        _mockJobRepository
            .Setup(r => r.GetLatestByVideoIdAsync(existingVideo.Id))
            .ReturnsAsync((Job?)null);

        // Act
        var result = await _videoService.SubmitVideoFromUrlAsync(submitDto, userId);

        // Assert
        result.Should().NotBeNull();
        result.VideoId.Should().Be(existingVideo.Id);
        result.JobId.Should().BeEmpty();
        result.IsExisting.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitVideoFromUrlAsync_WithNewVideo_CreatesVideoAndJob()
    {
        // Arrange
        var userId = "user-123";
        var youtubeId = "dQw4w9WgXcQ";
        var submitDto = new SubmitVideoDto { Url = $"https://www.youtube.com/watch?v={youtubeId}" };

        _mockVideoRepository
            .Setup(r => r.GetByYouTubeIdAsync(youtubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Video?)null);

        // Setup transaction to capture and execute the action
        _mockUnitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<VideoSubmissionResultDto>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<VideoSubmissionResultDto>>, CancellationToken>((func, ct) => func());

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _videoService.SubmitVideoFromUrlAsync(submitDto, userId);

        // Assert
        result.Should().NotBeNull();
        result.VideoId.Should().NotBeNullOrEmpty();
        result.JobId.Should().NotBeNullOrEmpty();
        result.YouTubeId.Should().Be(youtubeId);
        result.IsExisting.Should().BeFalse();
        result.Message.Should().Contain("successfully");
        result.Title.Should().NotBeNullOrEmpty();

        // Verify video was added
        _mockVideoRepository.Verify(r => r.AddAsync(It.Is<Video>(v =>
            v.YouTubeId == youtubeId &&
            v.UserId == userId &&
            v.Status == VideoStatus.Pending &&
            v.Url == $"https://www.youtube.com/watch?v={youtubeId}" &&
            v.OriginalUrl == submitDto.Url
        )), Times.Once);

        // Verify job was created
        _mockJobRepository.Verify(r => r.AddAsync(It.Is<Job>(j =>
            j.Type == JobType.VideoProcessing &&
            j.Status == JobStatus.Pending &&
            j.UserId == userId &&
            j.MaxRetries == 3 &&
            j.Progress == 0
        )), Times.Once);

        // Verify transaction was committed
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitVideoFromUrlAsync_WhenMetadataExtractionSucceeds_ReturnsResult()
    {
        // Arrange
        var userId = "user-123";
        var youtubeId = "dQw4w9WgXcQ"; // Real YouTube video that should work
        var submitDto = new SubmitVideoDto { Url = $"https://www.youtube.com/watch?v={youtubeId}" };

        _mockVideoRepository
            .Setup(r => r.GetByYouTubeIdAsync(youtubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Video?)null);

        // Setup transaction to execute the action
        _mockUnitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<VideoSubmissionResultDto>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<VideoSubmissionResultDto>>, CancellationToken>((func, ct) => func());

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _videoService.SubmitVideoFromUrlAsync(submitDto, userId);

        // Assert
        // The method will extract metadata from YouTube successfully
        result.Should().NotBeNull();
        result.YouTubeId.Should().Be(youtubeId);
        result.Title.Should().NotBeNullOrEmpty();
        result.IsExisting.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitVideoFromUrlAsync_WithTransactionRollback_DoesNotCreateVideoOrJob()
    {
        // Arrange
        var userId = "user-123";
        var youtubeId = "dQw4w9WgXcQ";
        var submitDto = new SubmitVideoDto { Url = $"https://www.youtube.com/watch?v={youtubeId}" };

        _mockVideoRepository
            .Setup(r => r.GetByYouTubeIdAsync(youtubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Video?)null);

        // Setup transaction to throw exception (simulating rollback)
        _mockUnitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<VideoSubmissionResultDto>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Transaction failed"));

        // Act
        var act = async () => await _videoService.SubmitVideoFromUrlAsync(submitDto, userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Transaction failed");

        // Transaction should have been attempted
        _mockUnitOfWork.Verify(u => u.ExecuteInTransactionAsync(
            It.IsAny<Func<Task<VideoSubmissionResultDto>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
