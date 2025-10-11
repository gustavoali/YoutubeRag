using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace YoutubeRag.Api.Controllers;

[ApiController]
[Route("api/v1/files")]
[Tags("📁 Files")]
[Authorize]
public class FilesController : ControllerBase
{
    /// <summary>
    /// Upload file for processing
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult> UploadFile(
        IFormFile file,
        string? title = null,
        string? description = null,
        string fileType = "video")
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = new { code = "NO_FILE", message = "No file uploaded" } });
        }

        // Validate file size (example: 500MB limit)
        if (file.Length > 500 * 1024 * 1024)
        {
            return BadRequest(new { error = new { code = "FILE_TOO_LARGE", message = "File size exceeds 500MB limit" } });
        }

        // Validate file type
        var allowedExtensions = new[] { ".mp4", ".avi", ".mov", ".mkv", ".webm", ".mp3", ".wav", ".m4a" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(fileExtension))
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "INVALID_FILE_TYPE",
                    message = $"File type '{fileExtension}' not supported. Allowed types: {string.Join(", ", allowedExtensions)}"
                }
            });
        }

        var fileId = Guid.NewGuid().ToString();
        var uploadPath = $"/uploads/{DateTime.UtcNow:yyyy/MM/dd}/{fileId}{fileExtension}";

        return Ok(new
        {
            file_id = fileId,
            original_name = file.FileName,
            title = title ?? Path.GetFileNameWithoutExtension(file.FileName),
            description,
            file_type = fileType,
            size_bytes = file.Length,
            upload_path = uploadPath,
            status = "uploaded",
            mime_type = file.ContentType,
            uploaded_at = DateTime.UtcNow,
            message = "File uploaded successfully"
        });
    }

    /// <summary>
    /// Get file information
    /// </summary>
    [HttpGet("{fileId}")]
    public async Task<ActionResult> GetFile(string fileId)
    {
        return Ok(new
        {
            id = fileId,
            original_name = "sample_video.mp4",
            title = "Sample Video",
            description = "A sample video file",
            file_type = "video",
            size_bytes = 52428800, // 50MB
            upload_path = $"/uploads/2024/09/28/{fileId}.mp4",
            status = "processed",
            mime_type = "video/mp4",
            uploaded_at = DateTime.UtcNow.AddHours(-2),
            processed_at = DateTime.UtcNow.AddHours(-1),
            metadata = new
            {
                duration_seconds = 300,
                resolution = "1920x1080",
                frame_rate = 30,
                bitrate = 1500000,
                codec = "h264"
            },
            processing_info = new
            {
                video_id = "video_123",
                job_id = "job_456",
                transcript_ready = true,
                embeddings_ready = true
            }
        });
    }

    /// <summary>
    /// Download file
    /// </summary>
    [HttpGet("{fileId}/download")]
    public async Task<ActionResult> DownloadFile(string fileId, string? version = null)
    {
        // In a real implementation, this would stream the actual file
        return Ok(new
        {
            file_id = fileId,
            download_url = $"https://api.youtuberag.com/files/{fileId}/stream",
            version = version ?? "original",
            expires_at = DateTime.UtcNow.AddHours(1),
            message = "Use the download_url to access the file"
        });
    }

    /// <summary>
    /// List user's uploaded files
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> ListFiles(
        int page = 1,
        int pageSize = 20,
        string? fileType = null,
        string? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var files = new[]
        {
            new {
                id = "file_1",
                original_name = "machine_learning_tutorial.mp4",
                title = "Machine Learning Tutorial",
                file_type = "video",
                size_bytes = 125829120,
                status = "processed",
                uploaded_at = DateTime.UtcNow.AddDays(-2),
                video_id = "video_1"
            },
            new {
                id = "file_2",
                original_name = "python_basics.mp4",
                title = "Python Basics",
                file_type = "video",
                size_bytes = 89478291,
                status = "processing",
                uploaded_at = DateTime.UtcNow.AddHours(-4),
                video_id = "video_2"
            },
            new {
                id = "file_3",
                original_name = "podcast_episode.mp3",
                title = "Data Science Podcast",
                file_type = "audio",
                size_bytes = 45789123,
                status = "failed",
                uploaded_at = DateTime.UtcNow.AddDays(-1),
                video_id = (string?)null
            }
        };

        return Ok(new
        {
            files,
            total = files.Length,
            page,
            page_size = pageSize,
            has_more = false,
            filters = new
            {
                file_type = fileType,
                status,
                from_date = fromDate,
                to_date = toDate
            }
        });
    }

    /// <summary>
    /// Delete uploaded file
    /// </summary>
    [HttpDelete("{fileId}")]
    public async Task<ActionResult> DeleteFile(string fileId, bool deleteAssociatedVideo = false)
    {
        return Ok(new
        {
            file_id = fileId,
            deleted_at = DateTime.UtcNow,
            deleted_associated_video = deleteAssociatedVideo,
            message = "File deleted successfully"
        });
    }

    /// <summary>
    /// Bulk file operations
    /// </summary>
    [HttpPost("bulk")]
    public async Task<ActionResult> BulkFileOperation([FromBody] BulkFileRequest request)
    {
        var results = request.FileIds.Select(fileId => new
        {
            file_id = fileId,
            operation = request.Operation,
            status = "success",
            message = $"Operation '{request.Operation}' completed successfully"
        });

        return Ok(new
        {
            operation = request.Operation,
            results,
            successful_count = results.Count(),
            failed_count = 0
        });
    }

    /// <summary>
    /// Get file processing status
    /// </summary>
    [HttpGet("{fileId}/status")]
    public async Task<ActionResult> GetFileProcessingStatus(string fileId)
    {
        return Ok(new
        {
            file_id = fileId,
            status = "processing",
            progress = 65,
            current_stage = "transcription",
            stages = new[] {
                new {
                    name = "upload",
                    status = "completed",
                    progress = 100,
                    started_at = (DateTime?)DateTime.UtcNow.AddMinutes(-30),
                    completed_at = (DateTime?)DateTime.UtcNow.AddMinutes(-28),
                    duration_seconds = (int?)120
                },
                new {
                    name = "validation",
                    status = "completed",
                    progress = 100,
                    started_at = (DateTime?)DateTime.UtcNow.AddMinutes(-28),
                    completed_at = (DateTime?)DateTime.UtcNow.AddMinutes(-25),
                    duration_seconds = (int?)180
                },
                new {
                    name = "audio_extraction",
                    status = "completed",
                    progress = 100,
                    started_at = (DateTime?)DateTime.UtcNow.AddMinutes(-25),
                    completed_at = (DateTime?)DateTime.UtcNow.AddMinutes(-20),
                    duration_seconds = (int?)300
                },
                new {
                    name = "transcription",
                    status = "running",
                    progress = 65,
                    started_at = (DateTime?)DateTime.UtcNow.AddMinutes(-20),
                    completed_at = (DateTime?)null,
                    duration_seconds = (int?)null
                },
                new {
                    name = "embedding_generation",
                    status = "pending",
                    progress = 0,
                    started_at = (DateTime?)null,
                    completed_at = (DateTime?)null,
                    duration_seconds = (int?)null
                }
            },
            estimated_completion = DateTime.UtcNow.AddMinutes(8),
            error_message = (string?)null
        });
    }

    /// <summary>
    /// Get storage usage statistics
    /// </summary>
    [HttpGet("storage/stats")]
    public async Task<ActionResult> GetStorageStats()
    {
        return Ok(new
        {
            usage = new
            {
                total_files = 47,
                total_size_bytes = 2583291904, // ~2.4GB
                total_size_gb = 2.4,
                videos = new
                {
                    count = 32,
                    size_bytes = 2198472192,
                    size_gb = 2.05
                },
                audio = new
                {
                    count = 15,
                    size_bytes = 384819712,
                    size_gb = 0.35
                }
            },
            quota = new
            {
                limit_gb = 10,
                used_percentage = 24.0,
                remaining_gb = 7.6
            },
            breakdown_by_month = new[] {
                new {
                    month = DateTime.UtcNow.AddMonths(-2).ToString("yyyy-MM"),
                    files = 12,
                    size_gb = 0.8
                },
                new {
                    month = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM"),
                    files = 18,
                    size_gb = 1.1
                },
                new {
                    month = DateTime.UtcNow.ToString("yyyy-MM"),
                    files = 17,
                    size_gb = 0.5
                }
            },
            cleanup_suggestions = new[] {
                new {
                    category = "failed_uploads",
                    count = 3,
                    size_gb = 0.2,
                    description = "Files that failed processing"
                },
                new {
                    category = "duplicates",
                    count = 2,
                    size_gb = 0.1,
                    description = "Potential duplicate files"
                }
            }
        });
    }

    /// <summary>
    /// Get supported file formats and limits
    /// </summary>
    [HttpGet("formats")]
    public async Task<ActionResult> GetSupportedFormats()
    {
        return Ok(new
        {
            video_formats = new[] {
                new { extension = ".mp4", mime_type = "video/mp4", max_size_mb = 500 },
                new { extension = ".avi", mime_type = "video/x-msvideo", max_size_mb = 500 },
                new { extension = ".mov", mime_type = "video/quicktime", max_size_mb = 500 },
                new { extension = ".mkv", mime_type = "video/x-matroska", max_size_mb = 500 },
                new { extension = ".webm", mime_type = "video/webm", max_size_mb = 500 }
            },
            audio_formats = new[] {
                new { extension = ".mp3", mime_type = "audio/mpeg", max_size_mb = 100 },
                new { extension = ".wav", mime_type = "audio/wav", max_size_mb = 100 },
                new { extension = ".m4a", mime_type = "audio/mp4", max_size_mb = 100 }
            },
            global_limits = new
            {
                max_files_per_user = 1000,
                max_storage_gb = 10,
                max_upload_size_mb = 500,
                concurrent_uploads = 3
            },
            processing_capabilities = new
            {
                video_transcription = true,
                audio_extraction = true,
                embedding_generation = true,
                thumbnail_generation = true,
                metadata_extraction = true
            }
        });
    }
}

public class BulkFileRequest
{
    public string[] FileIds { get; set; } = Array.Empty<string>();
    public string Operation { get; set; } = string.Empty; // delete, reprocess, move
    public Dictionary<string, object>? Parameters { get; set; }
}
