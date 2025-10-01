using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using YoutubeRag.Domain.Enums;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Application.Interfaces.Services;
using YoutubeRag.Application.DTOs.Video;
using YoutubeRag.Application.Exceptions;
using YoutubeRag.Api.Configuration;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace YoutubeRag.Api.Controllers;

[ApiController]
[Route("api/v1/videos")]
[Tags("🎥 Videos")]
[Authorize]
public class VideosController : ControllerBase
{
    private readonly IVideoProcessingService _videoProcessingService;
    private readonly IVideoService _videoService;
    private readonly AppSettings _appSettings;

    public VideosController(
        IVideoProcessingService videoProcessingService,
        IVideoService videoService,
        IOptions<AppSettings> appSettings)
    {
        _videoProcessingService = videoProcessingService;
        _videoService = videoService;
        _appSettings = appSettings.Value;
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

            return Ok(new {
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

        return Ok(new {
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

            return Ok(new {
                id = video.Id,
                title = video.Title,
                description = video.Description,
                url = request.Url,
                youtube_id = video.YoutubeId,
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
            return BadRequest(new {
                error = new {
                    code = "PROCESSING_ERROR",
                    message = ex.Message
                }
            });
        }
    }

    /// <summary>
    /// Get detailed progress information for a video
    /// </summary>
    [HttpGet("{videoId}/progress")]
    public async Task<ActionResult> GetVideoProgress(string videoId)
    {
        try
        {
            var progress = await _videoProcessingService.GetProcessingProgressAsync(videoId);

            return Ok(new {
                video_id = progress.VideoId,
                status = progress.Status.ToString(),
                progress = progress.OverallProgress,
                current_stage = progress.CurrentStage,
                stages = progress.Stages.Select(stage => new {
                    name = stage.Name,
                    status = stage.Status,
                    progress = stage.Progress,
                    started_at = stage.StartedAt,
                    completed_at = stage.CompletedAt,
                    error_message = stage.ErrorMessage
                }),
                estimated_completion = progress.EstimatedCompletion,
                error_message = progress.ErrorMessage,
                mode = _appSettings.UseRealProcessing ? "real" : "mock"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new {
                error = new {
                    code = "PROGRESS_ERROR",
                    message = ex.Message
                }
            });
        }
    }

    /// <summary>
    /// Get video details
    /// </summary>
    [HttpGet("{videoId}")]
    public async Task<ActionResult> GetVideo(string videoId)
    {
        try
        {
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
    public async Task<ActionResult> UpdateVideo(string videoId, [FromBody] UpdateVideoDto updateDto)
    {
        try
        {
            var video = await _videoService.UpdateAsync(videoId, updateDto);
            return Ok(new {
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
        return Ok(new {
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

        return Ok(new {
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
        return Ok(new {
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
        return Ok(new {
            message = "Debug endpoint working",
            sample_progress = new {
                status = "processing",
                progress = 65,
                current_stage = "transcription"
            }
        });
    }
}

public class VideoUrlRequest
{
    public string Url { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Description { get; set; }
}

public class ProcessingConfigRequest
{
    public bool ExtractAudio { get; set; } = true;
    public bool GenerateTranscript { get; set; } = true;
    public bool GenerateEmbeddings { get; set; } = true;
    public string? Language { get; set; }
}