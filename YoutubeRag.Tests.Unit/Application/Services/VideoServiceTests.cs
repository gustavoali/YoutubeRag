using AutoMapper;
using FluentAssertions;
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
        _mockVideoRepository = new Mock<IVideoRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockTranscriptRepository = new Mock<ITranscriptSegmentRepository>();
        _mockJobRepository = new Mock<IJobRepository>();

        // Setup UnitOfWork to return mock repositories
        _mockUnitOfWork.Setup(u => u.Videos).Returns(_mockVideoRepository.Object);
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
        _mockUnitOfWork.Setup(u => u.TranscriptSegments).Returns(_mockTranscriptRepository.Object);
        _mockUnitOfWork.Setup(u => u.Jobs).Returns(_mockJobRepository.Object);

        _videoService = new VideoService(
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockLogger.Object);
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
}
