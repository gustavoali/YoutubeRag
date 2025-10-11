using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using YoutubeRag.Api.Configuration;
using YoutubeRag.Application.DTOs.Progress;
using YoutubeRag.Application.DTOs.Video;
using YoutubeRag.Application.Exceptions;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Application.Interfaces.Services;
using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Api.Controllers;

[ApiController]
[Route("api/v1/videos")]
[Tags("🎥 Videos")]
[Authorize]
public class VideosController : ControllerBase
{
    private readonly IVideoProcessingService _videoProcessingService;
    private readonly IVideoService _videoService;
    private readonly IVideoIngestionService _videoIngestionService;
    private readonly IJobRepository _jobRepository;
    private readonly IVideoRepository _videoRepository;
    private readonly IMemoryCache _cache;
    private readonly AppSettings _appSettings;
    private readonly ILogger<VideosController> _logger;

    public VideosController(
        IVideoProcessingService videoProcessingService,
        IVideoService videoService,
        IVideoIngestionService videoIngestionService,
        IJobRepository jobRepository,
        IVideoRepository videoRepository,
        IMemoryCache cache,
        IOptions<AppSettings> appSettings,
        ILogger<VideosController> logger)
    {
        _videoProcessingService = videoProcessingService;
        _videoService = videoService;
        _videoIngestionService = videoIngestionService;
        _jobRepository = jobRepository;
        _videoRepository = videoRepository;
        _cache = cache;
        _appSettings = appSettings.Value;
        _logger = logger;
    }
    /// <summary>
    /// List user's videos with filtering and pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> ListVideos(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        VideoStatus[]? status = null,
        string sortBy = "created_at",
        string sortOrder = "desc")
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _videoService.GetAllAsync(page, pageSize, userId);

            return Ok(new
            {
                videos = result.Items,
                total = result.TotalCount,
                page = result.PageNumber,
                page_size = result.PageSize,
                total_pages = result.TotalPages,
                has_more = result.HasNext
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR", message = ex.Message } });
        }
    }

    /// <summary>
    /// Upload video file
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult> UploadVideo(IFormFile file, string? title = null, string? description = null)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = new { code = "NO_FILE", message = "No file uploaded" } });
        }

        // Mock video processing
        var videoId = Guid.NewGuid().ToString();

        return Ok(new
        {
            id = videoId,
            title = title ?? file.FileName,
            description,
            status = VideoStatus.Pending.ToString(),
            file_size = file.Length,
            message = "Video uploaded successfully, processing started"
        });
    }

    /// <summary>
    /// Process video from URL (YouTube, etc.)
    /// </summary>
    [HttpPost("from-url")]
    public async Task<ActionResult> ProcessVideoFromUrl([FromBody] VideoUrlRequest request)
    {
        if (string.IsNullOrEmpty(request.Url))
        {
            return BadRequest(new { error = new { code = "INVALID_URL", message = "URL is required" } });
        }

        try
        {
            var userId = User.Identity?.Name ?? "anonymous-user";

            var video = await _videoProcessingService.ProcessVideoFromUrlAsync(
                request.Url,
                request.Title,
                request.Description,
                userId);

            return Ok(new
            {
                id = video.Id,
                title = video.Title,
                description = video.Description,
                url = request.Url,
                youtube_id = video.YouTubeId,
                thumbnail_url = video.ThumbnailUrl,
                status = video.Status.ToString(),
                processing_progress = video.ProcessingProgress,
                message = _appSettings.UseRealProcessing
                    ? "Video processing from URL started - real processing"
                    : "Video processing from URL started - mock mode",
                created_at = video.CreatedAt
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "PROCESSING_ERROR",
                    message = ex.Message
                }
            });
        }
    }

    /// <summary>
    /// Get detailed progress information for a video
    /// </summary>
    /// <param name="videoId">The unique identifier of the video</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Video processing progress information</returns>
    /// <response code="200">Progress information retrieved successfully</response>
    /// <response code="404">Video not found or processing not started</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{videoId}/progress")]
    [ProducesResponseType(typeof(VideoProgressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VideoProgressResponse>> GetVideoProgress(
        string videoId,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var cacheKey = $"video_progress_{videoId}";

        try
        {
            // Check cache first
            if (_cache.TryGetValue(cacheKey, out VideoProgressResponse? cachedProgress))
            {
                _logger.LogDebug(
                    "Cache hit for video progress. VideoId: {VideoId}, ElapsedMs: {ElapsedMs}",
                    videoId,
                    stopwatch.ElapsedMilliseconds);

                return Ok(cachedProgress);
            }

            _logger.LogDebug(
                "Cache miss for video progress. VideoId: {VideoId}, ElapsedMs: {ElapsedMs}",
                videoId,
                stopwatch.ElapsedMilliseconds);

            // Query video to verify it exists
            var video = await _videoRepository.GetByIdAsync(videoId);

            if (video == null)
            {
                _logger.LogWarning(
                    "Video not found. VideoId: {VideoId}, ElapsedMs: {ElapsedMs}",
                    videoId,
                    stopwatch.ElapsedMilliseconds);

                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Video Not Found",
                    Detail = "Video not found",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    Extensions =
                    {
                        ["videoId"] = videoId,
                        ["traceId"] = HttpContext.TraceIdentifier,
                        ["timestamp"] = DateTime.UtcNow
                    }
                });
            }

            // Query latest job for the video
            var job = await _jobRepository.GetLatestByVideoIdAsync(videoId);

            if (job == null)
            {
                _logger.LogWarning(
                    "Video exists but no processing job found. VideoId: {VideoId}, ElapsedMs: {ElapsedMs}",
                    videoId,
                    stopwatch.ElapsedMilliseconds);

                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Processing Not Started",
                    Detail = "Video exists but processing not started",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    Extensions =
                    {
                        ["videoId"] = videoId,
                        ["traceId"] = HttpContext.TraceIdentifier,
                        ["timestamp"] = DateTime.UtcNow
                    }
                });
            }

            // Map Job entity to VideoProgressResponse DTO
            var progressResponse = MapJobToProgressResponse(job, videoId);

            // Cache the response for 5 seconds
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(5));

            _cache.Set(cacheKey, progressResponse, cacheOptions);

            _logger.LogInformation(
                "Video progress retrieved successfully. VideoId: {VideoId}, JobId: {JobId}, Status: {Status}, Progress: {Progress}, ElapsedMs: {ElapsedMs}",
                videoId,
                job.Id,
                job.Status,
                job.Progress,
                stopwatch.ElapsedMilliseconds);

            return Ok(progressResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving video progress. VideoId: {VideoId}, ElapsedMs: {ElapsedMs}",
                videoId,
                stopwatch.ElapsedMilliseconds);

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving video progress",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Extensions =
                {
                    ["videoId"] = videoId,
                    ["traceId"] = HttpContext.TraceIdentifier,
                    ["timestamp"] = DateTime.UtcNow
                }
            });
        }
    }

    /// <summary>
    /// Maps a Job entity to a VideoProgressResponse DTO
    /// </summary>
    /// <param name="job">The job entity</param>
    /// <param name="videoId">The video identifier</param>
    /// <returns>VideoProgressResponse DTO</returns>
    private VideoProgressResponse MapJobToProgressResponse(Domain.Entities.Job job, string videoId)
    {
        // Calculate estimated completion time based on progress
        DateTime? estimatedCompletion = null;
        if (job.StartedAt.HasValue && job.Progress > 0 && job.Progress < 100)
        {
            var elapsedTime = DateTime.UtcNow - job.StartedAt.Value;
            var estimatedTotalTime = elapsedTime.TotalSeconds / (job.Progress / 100.0);
            estimatedCompletion = job.StartedAt.Value.AddSeconds(estimatedTotalTime);
        }

        // Determine current stage based on job status and progress
        string currentStage = job.StatusMessage ?? DetermineCurrentStage(job);

        return new VideoProgressResponse
        {
            VideoId = videoId,
            JobId = job.Id,
            Status = job.Status.ToString(),
            ProgressPercentage = job.Progress,
            CurrentStage = currentStage,
            HangfireJobId = job.HangfireJobId,
            StartedAt = job.StartedAt,
            EstimatedCompletion = estimatedCompletion,
            ErrorMessage = job.ErrorMessage,
            UpdatedAt = job.UpdatedAt
        };
    }

    /// <summary>
    /// Determines the current processing stage based on job properties
    /// </summary>
    /// <param name="job">The job entity</param>
    /// <returns>Current stage description</returns>
    private static string DetermineCurrentStage(Domain.Entities.Job job)
    {
        return job.Status switch
        {
            JobStatus.Pending => "Queued for processing",
            JobStatus.Running when job.Progress < 30 => "Extracting audio",
            JobStatus.Running when job.Progress < 70 => "Transcribing audio",
            JobStatus.Running when job.Progress < 90 => "Generating embeddings",
            JobStatus.Running => "Finalizing",
            JobStatus.Completed => "Completed",
            JobStatus.Failed => "Failed",
            JobStatus.Cancelled => "Cancelled",
            JobStatus.Retrying => "Retrying after error",
            _ => "Processing"
        };
    }

    /// <summary>
    /// Get video details
    /// </summary>
    [HttpGet("{videoId}")]
    public async Task<ActionResult> GetVideo(string videoId)
    {
        try
        {
            // Get the current user's ID from the authenticated context
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogDebug("GetVideo - Current user ID: {UserId}", userId);

            // First get the basic video info to check ownership
            var videoDto = await _videoService.GetByIdAsync(videoId);
            if (videoDto == null)
            {
                return NotFound(new { error = new { code = "NOT_FOUND", message = "Video not found" } });
            }

            _logger.LogDebug("GetVideo - Checking authorization. Video UserId: {VideoUserId}, Current UserId: {CurrentUserId}",
                videoDto.UserId, userId);

            // Check if the current user owns the video
            if (!string.IsNullOrEmpty(videoDto.UserId) && videoDto.UserId != userId)
            {
                _logger.LogWarning("GetVideo - Authorization denied. User {UserId} attempted to access video owned by {VideoUserId}",
                    userId, videoDto.UserId);
                return StatusCode(403, new { error = new { code = "FORBIDDEN", message = "You do not have permission to view this video" } });
            }

            var video = await _videoService.GetDetailsAsync(videoId);
            return Ok(video);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = ex.Message } });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR", message = ex.Message } });
        }
    }

    /// <summary>
    /// Update video metadata
    /// </summary>
    [HttpPatch("{videoId}")]
    [HttpPut("{videoId}")]
    public async Task<ActionResult> UpdateVideo(string videoId, [FromBody] UpdateVideoDto updateDto)
    {
        try
        {
            var video = await _videoService.UpdateAsync(videoId, updateDto);
            return Ok(new
            {
                id = video.Id,
                message = "Video updated successfully",
                video
            });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = ex.Message } });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR", message = ex.Message } });
        }
    }

    /// <summary>
    /// Delete video
    /// </summary>
    [HttpDelete("{videoId}")]
    public async Task<ActionResult> DeleteVideo(string videoId)
    {
        try
        {
            // Get the current user's ID from the authenticated context
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            _logger.LogDebug("DeleteVideo - Current user ID: {UserId}", userId);

            // Get the video to check ownership
            var video = await _videoService.GetByIdAsync(videoId);
            if (video == null)
            {
                return NotFound(new { error = new { code = "NOT_FOUND", message = "Video not found" } });
            }

            _logger.LogDebug("DeleteVideo - Checking authorization. Video UserId: {VideoUserId}, Current UserId: {CurrentUserId}",
                video.UserId, userId);

            // Check if the current user owns the video
            if (!string.IsNullOrEmpty(video.UserId) && video.UserId != userId)
            {
                _logger.LogWarning("DeleteVideo - Authorization denied. User {UserId} attempted to delete video owned by {VideoUserId}",
                    userId, video.UserId);
                return StatusCode(403, new { error = new { code = "FORBIDDEN", message = "You do not have permission to delete this video" } });
            }

            await _videoService.DeleteAsync(videoId);
            return Ok(new { message = "Video deleted successfully" });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = ex.Message } });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR", message = ex.Message } });
        }
    }

    /// <summary>
    /// Reprocess video with new configuration
    /// </summary>
    [HttpPost("{videoId}/reprocess")]
    public async Task<ActionResult> ReprocessVideo(string videoId, [FromBody] ProcessingConfigRequest config)
    {
        return Ok(new
        {
            video_id = videoId,
            job_id = Guid.NewGuid().ToString(),
            message = "Video reprocessing started",
            config
        });
    }

    /// <summary>
    /// Get video transcription segments
    /// </summary>
    [HttpGet("{videoId}/transcript")]
    public async Task<ActionResult> GetVideoTranscript(string videoId)
    {
        var segments = new[]
        {
            new {
                id = "1",
                start_time = 0.0,
                end_time = 3.5,
                text = "Welcome to this video tutorial",
                confidence = 0.95
            },
            new {
                id = "2",
                start_time = 3.5,
                end_time = 8.2,
                text = "Today we'll be learning about YouTube RAG",
                confidence = 0.92
            }
        };

        return Ok(new
        {
            video_id = videoId,
            segments,
            total_segments = segments.Length
        });
    }

    /// <summary>
    /// Get video transcription directly by YouTube ID (testing endpoint)
    /// </summary>
    [HttpGet("transcript-test/{youtubeId}")]
    public async Task<ActionResult> GetTranscriptByYoutubeId(string youtubeId)
    {
        return Ok(new
        {
            youtube_id = youtubeId,
            transcript = "Mock transcript for YouTube video",
            source = "youtube-api"
        });
    }

    /// <summary>
    /// Endpoint de depuración para probar la función get_processing_progress_info
    /// </summary>
    [HttpGet("debug/progress-test")]
    public async Task<ActionResult> DebugProgressTest()
    {
        return Ok(new
        {
            message = "Debug endpoint working",
            sample_progress = new
            {
                status = "processing",
                progress = 65,
                current_stage = "transcription"
            }
        });
    }

    /// <summary>
    /// Ingest a video from YouTube URL
    /// </summary>
    /// <response code="200">Video ingestion initiated successfully</response>
    /// <response code="400">Validation error in request</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="409">Video already exists</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("ingest")]
    [ProducesResponseType(typeof(VideoIngestionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VideoIngestionResponse>> IngestVideo(
        [FromBody] VideoUrlRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = "User ID not found in authentication token",
                    Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                    Extensions =
                    {
                        ["traceId"] = HttpContext.TraceIdentifier,
                        ["timestamp"] = DateTime.UtcNow
                    }
                });
            }

            var requestDto = new VideoIngestionRequestDto(
                Url: request.Url,
                UserId: userId,
                Title: request.Title,
                Description: request.Description,
                Priority: request.Priority
            );

            var response = await _videoIngestionService.IngestVideoFromUrlAsync(requestDto, cancellationToken);

            return Ok(response);
        }
        catch (BusinessValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for video ingestion request: {Url}", request.Url);

            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = ex.Message,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Extensions =
                {
                    ["errors"] = ex.Errors,
                    ["traceId"] = HttpContext.TraceIdentifier,
                    ["timestamp"] = DateTime.UtcNow
                }
            });
        }
        catch (DuplicateResourceException ex)
        {
            _logger.LogInformation(ex, "Duplicate video detected: {ResourceId}", ex.ResourceId);

            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Duplicate Resource",
                Detail = $"Video already exists with ID: {ex.ResourceId}",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                Extensions =
                {
                    ["resourceId"] = ex.ResourceId,
                    ["resourceType"] = ex.ResourceType,
                    ["traceId"] = HttpContext.TraceIdentifier,
                    ["timestamp"] = DateTime.UtcNow
                }
            });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error during video ingestion: {Url}", request.Url);

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Database Error",
                Detail = "Failed to save video to database. Please try again later.",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Extensions =
                {
                    ["traceId"] = HttpContext.TraceIdentifier,
                    ["timestamp"] = DateTime.UtcNow
                }
            });
        }
        catch (DatabaseException ex)
        {
            _logger.LogError(ex, "Database operation failed during video ingestion: {Url}", request.Url);

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Database Error",
                Detail = "Failed to save video to database. Please try again later.",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Extensions =
                {
                    ["traceId"] = HttpContext.TraceIdentifier,
                    ["timestamp"] = DateTime.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during video ingestion: {Url}", request.Url);

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred. Please contact support.",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Extensions =
                {
                    ["traceId"] = HttpContext.TraceIdentifier,
                    ["timestamp"] = DateTime.UtcNow
                }
            });
        }
    }
}

public class ProcessingConfigRequest
{
    public bool ExtractAudio { get; set; } = true;
    public bool GenerateTranscript { get; set; } = true;
    public bool GenerateEmbeddings { get; set; } = true;
    public string? Language { get; set; }
}
