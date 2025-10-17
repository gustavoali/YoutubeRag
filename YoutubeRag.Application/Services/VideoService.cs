using System.Text.RegularExpressions;
using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using YoutubeExplode;
using YoutubeRag.Application.DTOs.Common;
using YoutubeRag.Application.DTOs.Video;
using YoutubeRag.Application.Exceptions;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Application.Interfaces.Services;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Application.Services;

/// <summary>
/// Service implementation for video management operations
/// </summary>
public class VideoService : IVideoService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<VideoService> _logger;
    private readonly IMemoryCache _cache;

    // Static YoutubeClient to avoid creating new instances on each retry
    // YoutubeClient is thread-safe and can be reused across requests
    private static readonly YoutubeClient _youtubeClient = new YoutubeClient();

    public VideoService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<VideoService> logger,
        IMemoryCache cache)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _cache = cache;
    }

    public async Task<VideoDto?> GetByIdAsync(string id)
    {
        _logger.LogInformation("Getting video by ID: {VideoId}", id);

        var video = await _unitOfWork.Videos.GetByIdAsync(id);
        if (video == null)
        {
            _logger.LogWarning("Video not found: {VideoId}", id);
            return null;
        }

        return _mapper.Map<VideoDto>(video);
    }

    public async Task<PaginatedResultDto<VideoListDto>> GetAllAsync(int page = 1, int pageSize = 10, string? userId = null)
    {
        _logger.LogInformation("Getting all videos - Page: {Page}, PageSize: {PageSize}, UserId: {UserId}",
            page, pageSize, userId ?? "all");

        IEnumerable<Video> videos;
        int totalCount;

        if (!string.IsNullOrEmpty(userId))
        {
            videos = await _unitOfWork.Videos.FindAsync(v => v.UserId == userId);
            totalCount = videos.Count();
            videos = videos.Skip((page - 1) * pageSize).Take(pageSize);
        }
        else
        {
            totalCount = await _unitOfWork.Videos.CountAsync();
            var allVideos = await _unitOfWork.Videos.GetAllAsync();
            videos = allVideos.Skip((page - 1) * pageSize).Take(pageSize);
        }

        var videoDtos = _mapper.Map<List<VideoListDto>>(videos);

        return new PaginatedResultDto<VideoListDto>(
            videoDtos,
            page,
            pageSize,
            totalCount
        );
    }

    public async Task<VideoDto> CreateAsync(CreateVideoDto createDto, string userId)
    {
        _logger.LogInformation("Creating new video: {Title} for user: {UserId}", createDto.Title, userId);

        // Verify user exists
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            throw new EntityNotFoundException("User", userId);
        }

        var video = _mapper.Map<Video>(createDto);
        video.Id = Guid.NewGuid().ToString();
        video.UserId = userId;
        video.CreatedAt = DateTime.UtcNow;
        video.UpdatedAt = DateTime.UtcNow;
        video.Status = Domain.Enums.VideoStatus.Pending;

        await _unitOfWork.Videos.AddAsync(video);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Video created successfully: {VideoId}", video.Id);

        return _mapper.Map<VideoDto>(video);
    }

    public async Task<VideoDto> UpdateAsync(string id, UpdateVideoDto updateDto)
    {
        _logger.LogInformation("Updating video: {VideoId}", id);

        var video = await _unitOfWork.Videos.GetByIdAsync(id);
        if (video == null)
        {
            throw new EntityNotFoundException("Video", id);
        }

        // Update only provided fields
        if (!string.IsNullOrEmpty(updateDto.Title))
        {
            video.Title = updateDto.Title;
        }

        if (updateDto.Description != null)
        {
            video.Description = updateDto.Description;
        }

        if (updateDto.Status.HasValue)
        {
            video.Status = updateDto.Status.Value;
        }

        if (updateDto.Duration.HasValue)
        {
            video.Duration = updateDto.Duration.Value;
        }

        video.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Videos.UpdateAsync(video);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Video updated successfully: {VideoId}", id);

        return _mapper.Map<VideoDto>(video);
    }

    public async Task DeleteAsync(string id)
    {
        _logger.LogInformation("Deleting video: {VideoId}", id);

        var video = await _unitOfWork.Videos.GetByIdAsync(id);
        if (video == null)
        {
            throw new EntityNotFoundException("Video", id);
        }

        await _unitOfWork.Videos.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Video deleted successfully: {VideoId}", id);
    }

    public async Task<VideoDetailsDto> GetDetailsAsync(string id)
    {
        _logger.LogInformation("Getting video details: {VideoId}", id);

        var video = await _unitOfWork.Videos.GetByIdAsync(id);
        if (video == null)
        {
            throw new EntityNotFoundException("Video", id);
        }

        // Load related entities manually
        // Note: In a real implementation, you might want to use Include/ThenInclude with EF Core
        // For now, the mapper will handle the navigation properties if they're loaded

        return _mapper.Map<VideoDetailsDto>(video);
    }

    public async Task<VideoStatsDto> GetStatsAsync(string id)
    {
        _logger.LogInformation("Getting stats for video: {VideoId}", id);

        var video = await _unitOfWork.Videos.GetByIdAsync(id);
        if (video == null)
        {
            throw new EntityNotFoundException("Video", id);
        }

        var transcripts = await _unitOfWork.TranscriptSegments.FindAsync(t => t.VideoId == id);
        var jobs = await _unitOfWork.Jobs.FindAsync(j => j.VideoId == id);

        var stats = new VideoStatsDto(
            Id: id,
            TotalTranscriptSegments: transcripts.Count(),
            TotalJobs: jobs.Count(),
            CompletedJobs: jobs.Count(j => j.Status == Domain.Enums.JobStatus.Completed),
            FailedJobs: jobs.Count(j => j.Status == Domain.Enums.JobStatus.Failed),
            AverageConfidence: transcripts.Any() ? transcripts.Average(t => t.Confidence ?? 0) : 0,
            Duration: video.Duration
        );

        return stats;
    }

    public async Task<List<VideoListDto>> GetByUserIdAsync(string userId)
    {
        _logger.LogInformation("Getting videos for user: {UserId}", userId);

        var videos = await _unitOfWork.Videos.FindAsync(v => v.UserId == userId);
        return _mapper.Map<List<VideoListDto>>(videos);
    }

    public async Task<VideoSubmissionResultDto> SubmitVideoFromUrlAsync(
        SubmitVideoDto submitDto,
        string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Submitting YouTube URL for user {UserId}: {Url}", userId, submitDto.Url);

        // Rate limiting check: max 10 submissions per minute per user
        // Thread-safe implementation using lock for atomic check-and-increment
        var rateLimitKey = $"video_submission_rate_limit:{userId}";
        var lockKey = $"video_submission_lock:{userId}";

        // Get or create the lock object for this user
        var lockObject = _cache.GetOrCreate(lockKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            entry.Priority = CacheItemPriority.Normal;
            return new object();
        });

        // Use lock to ensure atomic check-and-increment
        int newCount;
        lock (lockObject!)
        {
            var currentCount = _cache.GetOrCreate(rateLimitKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                return 0;
            });

            if (currentCount >= 10)
            {
                _logger.LogWarning("Rate limit exceeded for user {UserId}. Current count: {Count}", userId, currentCount);
                throw new InvalidOperationException("Rate limit exceeded. Maximum 10 video submissions per minute.");
            }

            newCount = currentCount + 1;
            _cache.Set(rateLimitKey, newCount, TimeSpan.FromMinutes(1));
        }

        _logger.LogDebug("Rate limit check passed for user {UserId}. Submissions: {Count}/10", userId, newCount);

        // AC1: URL Validation
        var youtubeId = ValidateAndExtractYouTubeId(submitDto.Url);

        // AC2: Duplicate Detection
        var existingVideo = await _unitOfWork.Videos.GetByYouTubeIdAsync(youtubeId, cancellationToken);
        if (existingVideo != null)
        {
            _logger.LogInformation("Video already exists: {VideoId}, YouTubeId: {YouTubeId}", existingVideo.Id, youtubeId);

            // Get the latest job for this video
            var existingJob = await _unitOfWork.Jobs.GetLatestByVideoIdAsync(existingVideo.Id);

            return new VideoSubmissionResultDto
            {
                VideoId = existingVideo.Id,
                JobId = existingJob?.Id ?? string.Empty,
                Title = existingVideo.Title,
                Duration = existingVideo.Duration,
                Author = existingVideo.ChannelTitle,
                ThumbnailUrl = existingVideo.ThumbnailUrl,
                YouTubeId = youtubeId,
                IsExisting = true,
                Message = "Video already processed. Returning existing video record."
            };
        }

        // AC3 & AC4: Metadata Extraction and Job Creation (in transaction)
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // AC3: Extract metadata using YoutubeExplode with retry logic
            var metadata = await ExtractVideoMetadataAsync(youtubeId, cancellationToken);

            // Create Video entity
            var video = new Video
            {
                Id = Guid.NewGuid().ToString(),
                Title = metadata.Title,
                Description = metadata.Description,
                YouTubeId = youtubeId,
                Url = $"https://www.youtube.com/watch?v={youtubeId}",
                OriginalUrl = submitDto.Url,
                ThumbnailUrl = metadata.ThumbnailUrl,
                Duration = metadata.Duration,
                ChannelTitle = metadata.Author,
                ChannelId = metadata.ChannelId,
                PublishedAt = metadata.PublishedAt,
                UserId = userId,
                Status = VideoStatus.Pending,
                ProcessingStatus = VideoStatus.Pending,
                TranscriptionStatus = TranscriptionStatus.NotStarted,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Videos.AddAsync(video);

            // AC4: Create Job entity with "Pending" status
            var job = new Job
            {
                Id = Guid.NewGuid().ToString(),
                Type = JobType.VideoProcessing,
                Status = JobStatus.Pending,
                StatusMessage = "Job created, waiting for background processing",
                Progress = 0,
                CurrentStage = PipelineStage.None,
                UserId = userId,
                VideoId = video.Id,
                MaxRetries = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Jobs.AddAsync(job);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Video and job created successfully. VideoId: {VideoId}, JobId: {JobId}, YouTubeId: {YouTubeId}",
                video.Id, job.Id, youtubeId);

            return new VideoSubmissionResultDto
            {
                VideoId = video.Id,
                JobId = job.Id,
                Title = video.Title,
                Duration = video.Duration,
                Author = video.ChannelTitle,
                ThumbnailUrl = video.ThumbnailUrl,
                YouTubeId = youtubeId,
                IsExisting = false,
                Message = "Video submitted successfully for processing"
            };
        }, cancellationToken);
    }

    /// <summary>
    /// Validates YouTube URL and extracts video ID
    /// Supports both youtube.com and youtu.be formats
    /// </summary>
    private string ValidateAndExtractYouTubeId(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL cannot be empty", nameof(url));
        }

        // Add length check to prevent ReDoS
        if (url.Length > 2048)
        {
            throw new ArgumentException("URL exceeds maximum allowed length (2048 characters)", nameof(url));
        }

        // Use anchored regex patterns with timeout
        var patterns = new[]
        {
            @"^https?://(?:www\.)?youtube\.com/watch\?(?:.*&)?v=([a-zA-Z0-9_-]{11})(?:&.*)?$",
            @"^https?://youtu\.be/([a-zA-Z0-9_-]{11})(?:\?.*)?$",
            @"^https?://(?:www\.)?youtube\.com/embed/([a-zA-Z0-9_-]{11})(?:\?.*)?$",
            @"^https?://(?:www\.)?youtube\.com/v/([a-zA-Z0-9_-]{11})(?:\?.*)?$"
        };

        var timeout = TimeSpan.FromMilliseconds(100);

        foreach (var pattern in patterns)
        {
            try
            {
                var match = Regex.Match(url, pattern, RegexOptions.None, timeout);
                if (match.Success)
                {
                    var videoId = match.Groups[1].Value;
                    _logger.LogDebug("Extracted YouTube ID {YouTubeId} from URL", videoId);
                    return videoId;
                }
            }
            catch (RegexMatchTimeoutException)
            {
                _logger.LogWarning("Regex timeout while validating URL");
                throw new ArgumentException("URL validation timeout - possibly malformed URL", nameof(url));
            }
        }

        throw new ArgumentException("Invalid YouTube URL format. Supported formats: youtube.com/watch?v=..., youtu.be/...", nameof(url));
    }

    /// <summary>
    /// Extracts video metadata from YouTube using YoutubeExplode with retry logic
    /// </summary>
    private async Task<VideoMetadata> ExtractVideoMetadataAsync(string youtubeId, CancellationToken cancellationToken)
    {
        // Custom exponential backoff: 10s, 30s, 90s
        var customRetryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                new[]
                {
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(90)
                },
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Retry {RetryCount} for YouTube metadata extraction after {Delay}s. YouTubeId: {YouTubeId}",
                        retryCount, timeSpan.TotalSeconds, youtubeId);
                });

        try
        {
            return await customRetryPolicy.ExecuteAsync(async () =>
            {
                var video = await _youtubeClient.Videos.GetAsync(youtubeId, cancellationToken);

                return new VideoMetadata
                {
                    Title = video.Title,
                    Description = video.Description,
                    Author = video.Author.ChannelTitle,
                    ChannelId = video.Author.ChannelId.Value,
                    Duration = video.Duration,
                    ThumbnailUrl = video.Thumbnails.OrderByDescending(t => t.Resolution.Area).FirstOrDefault()?.Url,
                    PublishedAt = video.UploadDate.DateTime
                };
            });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Failed to extract YouTube metadata after 3 retries. YouTubeId: {YouTubeId}", youtubeId);
            throw new InvalidOperationException($"Failed to extract YouTube metadata for video {youtubeId}. YouTube may be unavailable or the video may not exist.", ex);
        }
    }

    /// <summary>
    /// Internal class to hold extracted YouTube video metadata
    /// </summary>
    private class VideoMetadata
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string ChannelId { get; set; } = string.Empty;
        public TimeSpan? Duration { get; set; }
        public string? ThumbnailUrl { get; set; }
        public DateTime? PublishedAt { get; set; }
    }
}
