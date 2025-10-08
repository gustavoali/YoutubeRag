# Repository Pattern Integration Guide

## How to Register Repositories in Program.cs

To integrate the Repository Pattern and Unit of Work into your application, add the following to your `Program.cs` file:

### 1. Add the using statement at the top of Program.cs:

```csharp
using YoutubeRag.Infrastructure.DependencyInjection;
```

### 2. Register the repositories after the database configuration:

```csharp
// Database Configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Register Repository Pattern and Unit of Work
builder.Services.AddRepositories();
```

## Alternative: Manual Registration

If you prefer to register services manually in Program.cs, add these lines after the database configuration:

```csharp
// Add using statement at the top
using YoutubeRag.Infrastructure.Repositories;

// After database configuration, add:
// Repository and Unit of Work Registration
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IVideoRepository, VideoRepository>();
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<ITranscriptSegmentRepository, TranscriptSegmentRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```

## Usage Example in Controllers

Here's how to use the repositories in your controllers:

```csharp
using Microsoft.AspNetCore.Mvc;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Domain.Entities;

[ApiController]
[Route("api/[controller]")]
public class VideosController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<VideosController> _logger;

    public VideosController(IUnitOfWork unitOfWork, ILogger<VideosController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetVideo(string id)
    {
        var video = await _unitOfWork.Videos.GetWithTranscriptsAsync(id);
        if (video == null)
        {
            return NotFound();
        }
        return Ok(video);
    }

    [HttpPost]
    public async Task<IActionResult> CreateVideo([FromBody] CreateVideoDto dto)
    {
        // Using transaction
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var video = new Video
            {
                Title = dto.Title,
                YoutubeId = dto.YoutubeId,
                UserId = GetUserId() // Get from auth
            };

            await _unitOfWork.Videos.AddAsync(video);

            var job = new Job
            {
                VideoId = video.Id,
                UserId = video.UserId,
                JobType = "ProcessVideo",
                Status = JobStatus.Pending
            };

            await _unitOfWork.Jobs.AddAsync(job);
            await _unitOfWork.SaveChangesAsync();
        });

        return Ok();
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserVideos(string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var videos = await _unitOfWork.Videos.GetPaginatedByUserIdAsync(userId, page, pageSize);
        return Ok(videos);
    }
}
```

## Usage in Services

Example of using repositories in application services:

```csharp
public class VideoProcessingService : IVideoProcessingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<VideoProcessingService> _logger;

    public VideoProcessingService(IUnitOfWork unitOfWork, ILogger<VideoProcessingService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ProcessVideoAsync(string videoId)
    {
        var video = await _unitOfWork.Videos.GetByIdAsync(videoId);
        if (video == null)
        {
            throw new NotFoundException($"Video {videoId} not found");
        }

        // Check if job already exists
        if (await _unitOfWork.Jobs.HasActiveJobForVideoAsync(videoId))
        {
            _logger.LogWarning("Active job already exists for video {VideoId}", videoId);
            return;
        }

        // Create processing job
        var job = new Job
        {
            VideoId = videoId,
            UserId = video.UserId,
            JobType = "TranscribeVideo",
            Status = JobStatus.Pending
        };

        await _unitOfWork.Jobs.AddAsync(job);
        await _unitOfWork.SaveChangesAsync();

        // Process video...
    }

    public async Task<IEnumerable<TranscriptSegment>> SearchTranscriptsAsync(string searchText)
    {
        return await _unitOfWork.TranscriptSegments.SearchByTextAsync(searchText);
    }
}
```

## Testing with Repository Pattern

Example of unit testing with mocked repositories:

```csharp
[TestClass]
public class VideoServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork;
    private Mock<IVideoRepository> _mockVideoRepository;
    private VideoService _service;

    [TestInitialize]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockVideoRepository = new Mock<IVideoRepository>();
        _mockUnitOfWork.Setup(u => u.Videos).Returns(_mockVideoRepository.Object);

        _service = new VideoService(_mockUnitOfWork.Object);
    }

    [TestMethod]
    public async Task GetVideo_ReturnsVideo_WhenExists()
    {
        // Arrange
        var videoId = "test-id";
        var expectedVideo = new Video { Id = videoId, Title = "Test Video" };
        _mockVideoRepository.Setup(r => r.GetByIdAsync(videoId))
            .ReturnsAsync(expectedVideo);

        // Act
        var result = await _service.GetVideoAsync(videoId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedVideo.Id, result.Id);
        _mockVideoRepository.Verify(r => r.GetByIdAsync(videoId), Times.Once);
    }
}
```

## Benefits of This Implementation

1. **Separation of Concerns**: Data access logic is separated from business logic
2. **Testability**: Easy to mock repositories for unit testing
3. **Maintainability**: Changes to data access don't affect business logic
4. **Transaction Management**: Unit of Work pattern provides clean transaction handling
5. **Consistency**: All repositories follow the same pattern
6. **Performance**: Lazy loading of repositories in Unit of Work
7. **Type Safety**: Strong typing with generics