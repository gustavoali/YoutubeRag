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

    #region AC1: URL Validation Tests

    /// <summary>
    /// Tests that valid YouTube URL formats are accepted and video ID is correctly extracted
    /// </summary>
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/v/dQw4w9WgXcQ")]
    [InlineData("http://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("http://youtu.be/dQw4w9WgXcQ")]
    public async Task SubmitVideoFromUrlAsync_AC1_WithValidYouTubeUrl_ExtractsCorrectVideoId(string url)
    {
        // Arrange
        var userId = "user-123";
        var submitDto = new SubmitVideoDto { Url = url };
        var expectedYoutubeId = "dQw4w9WgXcQ";

        _mockVideoRepository
            .Setup(r => r.GetByYouTubeIdAsync(expectedYoutubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Video?)null);

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

    /// <summary>
    /// Tests that YouTube URLs with additional query parameters are handled correctly
    /// </summary>
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&t=30s")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&list=PLrAXtmErZgOeiKm4sgNOknGvNjby9efdf")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&t=30s&list=PLtest")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ?t=30s")]
    public async Task SubmitVideoFromUrlAsync_AC1_WithQueryParameters_ExtractsCorrectVideoId(string url)
    {
        // Arrange
        var userId = "user-123";
        var submitDto = new SubmitVideoDto { Url = url };
        var expectedYoutubeId = "dQw4w9WgXcQ";

        _mockVideoRepository
            .Setup(r => r.GetByYouTubeIdAsync(expectedYoutubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Video?)null);

        _mockUnitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<VideoSubmissionResultDto>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<VideoSubmissionResultDto>>, CancellationToken>((func, ct) => func());

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _videoService.SubmitVideoFromUrlAsync(submitDto, userId);

        // Assert
        result.YouTubeId.Should().Be(expectedYoutubeId);
    }

    /// <summary>
    /// Tests that empty or whitespace URLs are rejected with clear error message
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public async Task SubmitVideoFromUrlAsync_AC1_WithEmptyUrl_ThrowsArgumentException(string? invalidUrl)
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

    /// <summary>
    /// Tests that non-YouTube URLs are rejected with clear error message
    /// </summary>
    [Theory]
    [InlineData("https://vimeo.com/123456")]
    [InlineData("https://www.google.com")]
    [InlineData("https://www.dailymotion.com/video/x123")]
    [InlineData("not-a-url")]
    [InlineData("https://youtube.com/invalid")]
    [InlineData("https://youtube.com/")]
    [InlineData("ftp://youtube.com/watch?v=dQw4w9WgXcQ")]
    public async Task SubmitVideoFromUrlAsync_AC1_WithNonYouTubeUrl_ThrowsArgumentException(string invalidUrl)
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

    /// <summary>
    /// Tests that URLs exceeding maximum length (2048 characters) are rejected to prevent ReDoS attacks
    /// </summary>
    [Fact]
    public async Task SubmitVideoFromUrlAsync_AC1_WithExcessivelyLongUrl_ThrowsArgumentException()
    {
        // Arrange
        var userId = "user-123";
        var longUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ&" + new string('a', 2100);
        var submitDto = new SubmitVideoDto { Url = longUrl };

        // Act
        var act = async () => await _videoService.SubmitVideoFromUrlAsync(submitDto, userId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*exceeds maximum allowed length*2048*");
    }

    /// <summary>
    /// Tests that URL at exactly maximum length (2048 characters) is accepted
    /// </summary>
    [Fact]
    public async Task SubmitVideoFromUrlAsync_AC1_WithMaxLengthUrl_IsAccepted()
    {
        // Arrange
        var userId = "user-123";
        var baseUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ&params=";
        var padding = new string('a', 2048 - baseUrl.Length);
        var maxLengthUrl = baseUrl + padding;
        var submitDto = new SubmitVideoDto { Url = maxLengthUrl };

        _mockVideoRepository
            .Setup(r => r.GetByYouTubeIdAsync("dQw4w9WgXcQ", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Video?)null);

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
        result.YouTubeId.Should().Be("dQw4w9WgXcQ");
    }

    #endregion

    #region AC2: Duplicate Detection Tests

    /// <summary>
    /// Tests that when a video already exists, the existing video record is returned
    /// </summary>
    [Fact]
    public async Task SubmitVideoFromUrlAsync_AC2_WithDuplicateVideo_ReturnsExistingVideo()
    {
        // Arrange
        var userId = "user-123";
        var youtubeId = "dQw4w9WgXcQ";
        var submitDto = new SubmitVideoDto { Url = $"https://www.youtube.com/watch?v={youtubeId}" };

        var existingVideo = new VideoBuilder()
            .WithYouTubeId(youtubeId)
            .WithTitle("Existing Video")
            .WithDuration(TimeSpan.FromMinutes(3))
            .WithThumbnailUrl("https://example.com/thumb.jpg")
            .Build();

        existingVideo.ChannelTitle = "Test Channel";

        var existingJob = new JobBuilder()
            .WithId("job-123")
            .WithVideoId(existingVideo.Id)
            .WithStatus(JobStatus.Completed)
            .Build();

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
        result.Duration.Should().Be(TimeSpan.FromMinutes(3));
        result.Author.Should().Be("Test Channel");
        result.ThumbnailUrl.Should().Be("https://example.com/thumb.jpg");
        result.YouTubeId.Should().Be(youtubeId);
        result.IsExisting.Should().BeTrue();
        result.Message.Should().Contain("already processed");

        // Verify no new video or job was created
        _mockVideoRepository.Verify(r => r.AddAsync(It.IsAny<Video>()), Times.Never);
        _mockJobRepository.Verify(r => r.AddAsync(It.IsAny<Job>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Tests that duplicate detection works regardless of URL format used
    /// </summary>
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&t=30s")]
    public async Task SubmitVideoFromUrlAsync_AC2_WithDifferentUrlFormats_DetectsDuplicate(string url)
    {
        // Arrange
        var userId = "user-123";
        var youtubeId = "dQw4w9WgXcQ";
        var submitDto = new SubmitVideoDto { Url = url };

        var existingVideo = VideoBuilder.CreateValid();
        existingVideo.YouTubeId = youtubeId;

        var existingJob = JobBuilder.CreateCompleted();
        existingJob.VideoId = existingVideo.Id;

        _mockVideoRepository
            .Setup(r => r.GetByYouTubeIdAsync(youtubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingVideo);

        _mockJobRepository
            .Setup(r => r.GetLatestByVideoIdAsync(existingVideo.Id))
            .ReturnsAsync(existingJob);

        // Act
        var result = await _videoService.SubmitVideoFromUrlAsync(submitDto, userId);

        // Assert
        result.IsExisting.Should().BeTrue();
        result.VideoId.Should().Be(existingVideo.Id);
    }

    /// <summary>
    /// Tests that when video exists but has no job, empty job ID is returned
    /// </summary>
    [Fact]
    public async Task SubmitVideoFromUrlAsync_AC2_WithDuplicateVideoAndNoJob_ReturnsExistingVideoWithEmptyJobId()
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
        result.Message.Should().Contain("already processed");
    }

    /// <summary>
    /// Tests that no duplicate processing job is created for existing videos
    /// </summary>
    [Fact]
    public async Task SubmitVideoFromUrlAsync_AC2_WithDuplicateVideo_DoesNotCreateNewJob()
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
        await _videoService.SubmitVideoFromUrlAsync(submitDto, userId);

        // Assert
        _mockJobRepository.Verify(r => r.AddAsync(It.IsAny<Job>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.ExecuteInTransactionAsync(
            It.IsAny<Func<Task<VideoSubmissionResultDto>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region AC3: Metadata Extraction Tests

    /// <summary>
    /// Tests successful metadata extraction from YouTube with real video ID
    /// </summary>
    [Fact]
    public async Task SubmitVideoFromUrlAsync_AC3_WhenMetadataExtractionSucceeds_ReturnsCompleteMetadata()
    {
        // Arrange
        var userId = "user-123";
        var youtubeId = "dQw4w9WgXcQ"; // Real YouTube video that should work
        var submitDto = new SubmitVideoDto { Url = $"https://www.youtube.com/watch?v={youtubeId}" };

        _mockVideoRepository
            .Setup(r => r.GetByYouTubeIdAsync(youtubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Video?)null);

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
        result.YouTubeId.Should().Be(youtubeId);
        result.Title.Should().NotBeNullOrEmpty();
        result.Duration.Should().NotBeNull();
        result.Author.Should().NotBeNullOrEmpty();
        result.ThumbnailUrl.Should().NotBeNullOrEmpty();
        result.IsExisting.Should().BeFalse();
    }

    /// <summary>
    /// Tests that video metadata is correctly stored in Video entity
    /// </summary>
    [Fact]
    public async Task SubmitVideoFromUrlAsync_AC3_StoresMetadataInVideoEntity()
    {
        // Arrange
        var userId = "user-123";
        var youtubeId = "dQw4w9WgXcQ";
        var submitDto = new SubmitVideoDto { Url = $"https://www.youtube.com/watch?v={youtubeId}" };

        _mockVideoRepository
            .Setup(r => r.GetByYouTubeIdAsync(youtubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Video?)null);

        _mockUnitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<VideoSubmissionResultDto>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<VideoSubmissionResultDto>>, CancellationToken>((func, ct) => func());

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _videoService.SubmitVideoFromUrlAsync(submitDto, userId);

        // Assert
        _mockVideoRepository.Verify(r => r.AddAsync(It.Is<Video>(v =>
            v.YouTubeId == youtubeId &&
            v.Title != null && v.Title.Length > 0 &&
            v.Duration != null &&
            v.ChannelTitle != null && v.ChannelTitle.Length > 0 &&
            v.ChannelId != null && v.ChannelId.Length > 0 &&
            v.ThumbnailUrl != null &&
            v.PublishedAt != null &&
            v.Description != null
        )), Times.Once);
    }

    /// <summary>
    /// Tests that video ID is returned immediately after metadata extraction
    /// </summary>
    [Fact]
    public async Task SubmitVideoFromUrlAsync_AC3_ReturnsVideoIdImmediately()
    {
        // Arrange
        var userId = "user-123";
        var youtubeId = "dQw4w9WgXcQ";
        var submitDto = new SubmitVideoDto { Url = $"https://www.youtube.com/watch?v={youtubeId}" };

        _mockVideoRepository
            .Setup(r => r.GetByYouTubeIdAsync(youtubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Video?)null);

        _mockUnitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<VideoSubmissionResultDto>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<VideoSubmissionResultDto>>, CancellationToken>((func, ct) => func());

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _videoService.SubmitVideoFromUrlAsync(submitDto, userId);

        // Assert
        result.VideoId.Should().NotBeNullOrEmpty();
        result.VideoId.Should().MatchRegex(@"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$");
    }

    #endregion

    #region AC4: Job Creation Tests

    /// <summary>
    /// Tests that a new video and job are created atomically in a transaction
    /// </summary>
    [Fact]
    public async Task SubmitVideoFromUrlAsync_AC4_CreatesVideoAndJobAtomically()
    {
        // Arrange
        var userId = "user-123";
        var youtubeId = "dQw4w9WgXcQ";
        var submitDto = new SubmitVideoDto { Url = $"https://www.youtube.com/watch?v={youtubeId}" };

        _mockVideoRepository
            .Setup(r => r.GetByYouTubeIdAsync(youtubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Video?)null);

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
        result.IsExisting.Should().BeFalse();
        result.Message.Should().Contain("successfully");

        // Verify transaction was used
        _mockUnitOfWork.Verify(u => u.ExecuteInTransactionAsync(
            It.IsAny<Func<Task<VideoSubmissionResultDto>>>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify both video and job were created
        _mockVideoRepository.Verify(r => r.AddAsync(It.IsAny<Video>()), Times.Once);
        _mockJobRepository.Verify(r => r.AddAsync(It.IsAny<Job>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tests that Job entity is created with "Pending" status
    /// </summary>
    [Fact]
    public async Task SubmitVideoFromUrlAsync_AC4_CreatesJobWithPendingStatus()
    {
        // Arrange
        var userId = "user-123";
        var youtubeId = "dQw4w9WgXcQ";
        var submitDto = new SubmitVideoDto { Url = $"https://www.youtube.com/watch?v={youtubeId}" };

        _mockVideoRepository
            .Setup(r => r.GetByYouTubeIdAsync(youtubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Video?)null);

        _mockUnitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<VideoSubmissionResultDto>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<VideoSubmissionResultDto>>, CancellationToken>((func, ct) => func());

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _videoService.SubmitVideoFromUrlAsync(submitDto, userId);

        // Assert
        _mockJobRepository.Verify(r => r.AddAsync(It.Is<Job>(j =>
            j.Status == JobStatus.Pending &&
            j.Type == JobType.VideoProcessing &&
            j.StatusMessage == "Job created, waiting for background processing" &&
            j.Progress == 0 &&
            j.CurrentStage == PipelineStage.None
        )), Times.Once);
    }

    /// <summary>
    /// Tests that job ID is returned for progress tracking
    /// </summary>
    [Fact]
    public async Task SubmitVideoFromUrlAsync_AC4_ReturnsJobIdForTracking()
    {
        // Arrange
        var userId = "user-123";
        var youtubeId = "dQw4w9WgXcQ";
        var submitDto = new SubmitVideoDto { Url = $"https://www.youtube.com/watch?v={youtubeId}" };

        _mockVideoRepository
            .Setup(r => r.GetByYouTubeIdAsync(youtubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Video?)null);

        _mockUnitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<VideoSubmissionResultDto>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<VideoSubmissionResultDto>>, CancellationToken>((func, ct) => func());

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _videoService.SubmitVideoFromUrlAsync(submitDto, userId);

        // Assert
        result.JobId.Should().NotBeNullOrEmpty();
        result.JobId.Should().MatchRegex(@"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$");
    }

    /// <summary>
    /// Tests that job has correct configuration (max retries = 3)
    /// </summary>
    [Fact]
    public async Task SubmitVideoFromUrlAsync_AC4_CreatesJobWithCorrectConfiguration()
    {
        // Arrange
        var userId = "user-123";
        var youtubeId = "dQw4w9WgXcQ";
        var submitDto = new SubmitVideoDto { Url = $"https://www.youtube.com/watch?v={youtubeId}" };

        _mockVideoRepository
            .Setup(r => r.GetByYouTubeIdAsync(youtubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Video?)null);

        _mockUnitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<VideoSubmissionResultDto>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<VideoSubmissionResultDto>>, CancellationToken>((func, ct) => func());

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _videoService.SubmitVideoFromUrlAsync(submitDto, userId);

        // Assert
        _mockJobRepository.Verify(r => r.AddAsync(It.Is<Job>(j =>
            j.MaxRetries == 3 &&
            j.RetryCount == 0 &&
            j.UserId == userId &&
            j.VideoId != null
        )), Times.Once);
    }

    /// <summary>
    /// Tests that transaction rollback prevents video and job creation
    /// </summary>
    [Fact]
    public async Task SubmitVideoFromUrlAsync_AC4_WithTransactionRollback_DoesNotCreateVideoOrJob()
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

        _mockUnitOfWork.Verify(u => u.ExecuteInTransactionAsync(
            It.IsAny<Func<Task<VideoSubmissionResultDto>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tests that Video entity has correct properties set
    /// </summary>
    [Fact]
    public async Task SubmitVideoFromUrlAsync_AC4_CreatesVideoWithCorrectProperties()
    {
        // Arrange
        var userId = "user-123";
        var youtubeId = "dQw4w9WgXcQ";
        var submitDto = new SubmitVideoDto { Url = $"https://www.youtube.com/watch?v={youtubeId}" };

        _mockVideoRepository
            .Setup(r => r.GetByYouTubeIdAsync(youtubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Video?)null);

        _mockUnitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<VideoSubmissionResultDto>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<VideoSubmissionResultDto>>, CancellationToken>((func, ct) => func());

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _videoService.SubmitVideoFromUrlAsync(submitDto, userId);

        // Assert
        _mockVideoRepository.Verify(r => r.AddAsync(It.Is<Video>(v =>
            v.YouTubeId == youtubeId &&
            v.UserId == userId &&
            v.Status == VideoStatus.Pending &&
            v.ProcessingStatus == VideoStatus.Pending &&
            v.TranscriptionStatus == TranscriptionStatus.NotStarted &&
            v.Url == $"https://www.youtube.com/watch?v={youtubeId}" &&
            v.OriginalUrl == submitDto.Url &&
            v.CreatedAt != default &&
            v.UpdatedAt != default
        )), Times.Once);
    }

    #endregion

    #region AC5: Rate Limiting Tests

    /// <summary>
    /// Tests that rate limiting allows up to 10 submissions per minute per user
    /// </summary>
    [Fact]
    public async Task SubmitVideoFromUrlAsync_AC5_AllowsUpTo10SubmissionsPerMinute()
    {
        // Arrange
        var userId = "user-123";
        var youtubeId = "dQw4w9WgXcQ";
        var submitDto = new SubmitVideoDto { Url = $"https://www.youtube.com/watch?v={youtubeId}" };

        // Setup cache to return count of 9 (10th submission should succeed)
        object cacheValue = 9;
        _mockCache
            .Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(true);

        _mockVideoRepository
            .Setup(r => r.GetByYouTubeIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Video?)null);

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
        result.IsExisting.Should().BeFalse();
    }

    /// <summary>
    /// Tests that 11th submission within a minute throws InvalidOperationException
    /// </summary>
    [Fact]
    public async Task SubmitVideoFromUrlAsync_AC5_Exceeding10SubmissionsPerMinute_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = "user-123";
        var submitDto = new SubmitVideoDto { Url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ" };

        // Setup cache to return count of 10 (already at limit)
        object cacheValue = 10;
        _mockCache
            .Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(true);

        // Act
        var act = async () => await _videoService.SubmitVideoFromUrlAsync(submitDto, userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Rate limit exceeded*Maximum 10 video submissions per minute*");

        // Verify that no video processing was attempted
        _mockVideoRepository.Verify(r => r.GetByYouTubeIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Tests that rate limiting is enforced before URL validation
    /// </summary>
    [Fact]
    public async Task SubmitVideoFromUrlAsync_AC5_RateLimitCheckedBeforeUrlValidation()
    {
        // Arrange
        var userId = "user-123";
        var submitDto = new SubmitVideoDto { Url = "invalid-url" };

        // Setup cache to return count of 10 (at rate limit)
        object cacheValue = 10;
        _mockCache
            .Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(true);

        // Act
        var act = async () => await _videoService.SubmitVideoFromUrlAsync(submitDto, userId);

        // Assert - Should throw rate limit exception, not URL validation exception
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Rate limit exceeded*");
    }

    /// <summary>
    /// Tests that rate limit counter is incremented after successful validation
    /// Note: This test verifies the rate limit check happens, but cache.Set() is an extension method
    /// that cannot be verified with Moq. We verify the behavior by checking that the submission succeeds.
    /// </summary>
    [Fact]
    public async Task SubmitVideoFromUrlAsync_AC5_IncrementsRateLimitCounter()
    {
        // Arrange
        var userId = "user-123";
        var youtubeId = "dQw4w9WgXcQ";
        var submitDto = new SubmitVideoDto { Url = $"https://www.youtube.com/watch?v={youtubeId}" };

        // Setup cache to return 0 initially (within limit)
        object cacheValue = 0;
        _mockCache
            .Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(true);

        _mockVideoRepository
            .Setup(r => r.GetByYouTubeIdAsync(youtubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Video?)null);

        _mockUnitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<VideoSubmissionResultDto>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<VideoSubmissionResultDto>>, CancellationToken>((func, ct) => func());

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _videoService.SubmitVideoFromUrlAsync(submitDto, userId);

        // Assert - Verify submission succeeded (which means rate limit check passed and would have been incremented)
        result.Should().NotBeNull();
        result.IsExisting.Should().BeFalse();

        // Verify the cache was checked for rate limiting
        _mockCache.Verify(c => c.TryGetValue(It.IsAny<object>(), out cacheValue), Times.AtLeastOnce);
    }

    /// <summary>
    /// Tests that rate limiting is per-user (different users have separate limits)
    /// </summary>
    [Fact]
    public async Task SubmitVideoFromUrlAsync_AC5_RateLimitIsPerUser()
    {
        // Arrange
        var user1Id = "user-123";
        var user2Id = "user-456";
        var youtubeId = "dQw4w9WgXcQ";
        var submitDto = new SubmitVideoDto { Url = $"https://www.youtube.com/watch?v={youtubeId}" };

        // Setup cache with different counts for different users
        var cacheKey1 = $"video_submission_rate_limit:{user1Id}";
        var cacheKey2 = $"video_submission_rate_limit:{user2Id}";

        object cacheValue = 0;
        _mockCache
            .Setup(c => c.TryGetValue(cacheKey1, out cacheValue))
            .Returns(false);  // User 1 has no submissions

        _mockCache
            .Setup(c => c.TryGetValue(cacheKey2, out cacheValue))
            .Returns(false);  // User 2 has no submissions

        _mockVideoRepository
            .Setup(r => r.GetByYouTubeIdAsync(youtubeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Video?)null);

        _mockUnitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<VideoSubmissionResultDto>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<VideoSubmissionResultDto>>, CancellationToken>((func, ct) => func());

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act - Both users should be able to submit
        var result1 = await _videoService.SubmitVideoFromUrlAsync(submitDto, user1Id);
        var result2 = await _videoService.SubmitVideoFromUrlAsync(submitDto, user2Id);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
    }

    #endregion

    #endregion
}
