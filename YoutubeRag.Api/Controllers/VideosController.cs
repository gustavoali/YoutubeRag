using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Api.Controllers;

[ApiController]
[Route("api/v1/videos")]
[Tags("🎥 Videos")]
[Authorize]
public class VideosController : ControllerBase
{
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
        // Mock response
        var videos = new[]
        {
            new {
                id = "1",
                title = "Sample Video 1",
                description = "A sample video for testing",
                youtube_id = "dQw4w9WgXcQ",
                youtube_url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                thumbnail_url = "https://img.youtube.com/vi/dQw4w9WgXcQ/maxresdefault.jpg",
                duration = "00:03:33",
                status = VideoStatus.Completed.ToString(),
                created_at = DateTime.UtcNow.AddDays(-5),
                processing_progress = 100
            },
            new {
                id = "2",
                title = "Sample Video 2",
                description = "Another sample video",
                youtube_id = "oHg5SJYRHA0",
                youtube_url = "https://www.youtube.com/watch?v=oHg5SJYRHA0",
                thumbnail_url = "https://img.youtube.com/vi/oHg5SJYRHA0/maxresdefault.jpg",
                duration = "00:02:15",
                status = VideoStatus.Processing.ToString(),
                created_at = DateTime.UtcNow.AddDays(-2),
                processing_progress = 75
            }
        };

        return Ok(new {
            videos,
            total = videos.Length,
            page,
            page_size = pageSize,
            has_more = false
        });
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

        var videoId = Guid.NewGuid().ToString();

        return Ok(new {
            id = videoId,
            url = request.Url,
            status = VideoStatus.Pending.ToString(),
            message = "Video processing from URL started"
        });
    }

    /// <summary>
    /// Get detailed progress information for a video
    /// </summary>
    [HttpGet("{videoId}/progress")]
    public async Task<ActionResult> GetVideoProgress(string videoId)
    {
        return Ok(new {
            video_id = videoId,
            status = VideoStatus.Processing.ToString(),
            progress = 75,
            stages = new[] {
                new { name = "download", status = "completed", progress = 100 },
                new { name = "audio_extraction", status = "completed", progress = 100 },
                new { name = "transcription", status = "running", progress = 75 },
                new { name = "embedding", status = "pending", progress = 0 }
            },
            estimated_completion = DateTime.UtcNow.AddMinutes(5)
        });
    }

    /// <summary>
    /// Get video details
    /// </summary>
    [HttpGet("{videoId}")]
    public async Task<ActionResult> GetVideo(string videoId)
    {
        return Ok(new {
            id = videoId,
            title = "Sample Video",
            description = "A sample video",
            youtube_id = "dQw4w9WgXcQ",
            youtube_url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            thumbnail_url = "https://img.youtube.com/vi/dQw4w9WgXcQ/maxresdefault.jpg",
            duration = "00:03:33",
            status = VideoStatus.Completed.ToString(),
            created_at = DateTime.UtcNow.AddDays(-5),
            processing_progress = 100,
            transcript_segments_count = 45
        });
    }

    /// <summary>
    /// Update video metadata
    /// </summary>
    [HttpPatch("{videoId}")]
    public async Task<ActionResult> UpdateVideo(string videoId, [FromBody] Dictionary<string, object> updates)
    {
        return Ok(new {
            id = videoId,
            message = "Video updated successfully",
            updated_fields = updates.Keys
        });
    }

    /// <summary>
    /// Delete video
    /// </summary>
    [HttpDelete("{videoId}")]
    public async Task<ActionResult> DeleteVideo(string videoId)
    {
        return Ok(new { message = "Video deleted successfully" });
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