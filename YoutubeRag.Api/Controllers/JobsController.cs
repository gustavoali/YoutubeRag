using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Api.Controllers;

[ApiController]
[Route("api/v1/jobs")]
[Tags("⚙️ Jobs")]
[Authorize]
public class JobsController : ControllerBase
{
    /// <summary>
    /// List user's background jobs with filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> ListJobs(
        int page = 1,
        int pageSize = 20,
        JobStatus[]? status = null,
        string? jobType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        // Mock job list
        var jobs = new object[]
        {
            new {
                id = "job_1",
                job_type = "video_processing",
                status = JobStatus.Running.ToString(),
                video_id = "video_1",
                progress = 75,
                created_at = DateTime.UtcNow.AddMinutes(-30),
                started_at = DateTime.UtcNow.AddMinutes(-25),
                estimated_completion = DateTime.UtcNow.AddMinutes(5),
                current_stage = "transcription",
                error_message = (string?)null
            },
            new {
                id = "job_2",
                job_type = "embedding_generation",
                status = JobStatus.Completed.ToString(),
                video_id = "video_2",
                progress = 100,
                created_at = DateTime.UtcNow.AddHours(-2),
                started_at = DateTime.UtcNow.AddHours(-2),
                completed_at = DateTime.UtcNow.AddHours(-1),
                estimated_completion = (DateTime?)null,
                current_stage = "completed",
                error_message = (string?)null
            },
            new {
                id = "job_3",
                job_type = "video_download",
                status = JobStatus.Failed.ToString(),
                video_id = "video_3",
                progress = 0,
                created_at = DateTime.UtcNow.AddHours(-1),
                started_at = DateTime.UtcNow.AddHours(-1),
                failed_at = DateTime.UtcNow.AddMinutes(-55),
                estimated_completion = (DateTime?)null,
                current_stage = "download",
                error_message = "YouTube video not accessible"
            }
        };

        return Ok(new
        {
            jobs,
            total = jobs.Length,
            page,
            page_size = pageSize,
            has_more = false,
            filters = new
            {
                status,
                job_type = jobType,
                from_date = fromDate,
                to_date = toDate
            }
        });
    }

    /// <summary>
    /// Get detailed job information
    /// </summary>
    [HttpGet("{jobId}")]
    public async Task<ActionResult> GetJob(string jobId)
    {
        return Ok(new
        {
            id = jobId,
            job_type = "video_processing",
            status = JobStatus.Running.ToString(),
            video_id = "video_1",
            progress = 75,
            created_at = DateTime.UtcNow.AddMinutes(-30),
            started_at = DateTime.UtcNow.AddMinutes(-25),
            estimated_completion = DateTime.UtcNow.AddMinutes(5),
            current_stage = "transcription",
            stages = new[] {
                new {
                    name = "download",
                    status = "completed",
                    progress = 100,
                    started_at = (DateTime?)DateTime.UtcNow.AddMinutes(-25),
                    completed_at = (DateTime?)DateTime.UtcNow.AddMinutes(-20),
                    duration_seconds = (int?)300
                },
                new {
                    name = "audio_extraction",
                    status = "completed",
                    progress = 100,
                    started_at = (DateTime?)DateTime.UtcNow.AddMinutes(-20),
                    completed_at = (DateTime?)DateTime.UtcNow.AddMinutes(-15),
                    duration_seconds = (int?)300
                },
                new {
                    name = "transcription",
                    status = "running",
                    progress = 75,
                    started_at = (DateTime?)DateTime.UtcNow.AddMinutes(-15),
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
            logs = new[] {
                new {
                    timestamp = DateTime.UtcNow.AddMinutes(-25),
                    level = "info",
                    message = "Job started: video_processing",
                    stage = "initialization"
                },
                new {
                    timestamp = DateTime.UtcNow.AddMinutes(-20),
                    level = "info",
                    message = "Video download completed successfully",
                    stage = "download"
                },
                new {
                    timestamp = DateTime.UtcNow.AddMinutes(-15),
                    level = "info",
                    message = "Audio extraction completed",
                    stage = "audio_extraction"
                },
                new {
                    timestamp = DateTime.UtcNow.AddMinutes(-10),
                    level = "info",
                    message = "Transcription in progress: 75% complete",
                    stage = "transcription"
                }
            },
            error_message = (string?)null,
            retry_count = 0,
            max_retries = 3
        });
    }

    /// <summary>
    /// Cancel a running job
    /// </summary>
    [HttpPost("{jobId}/cancel")]
    public async Task<ActionResult> CancelJob(string jobId)
    {
        return Ok(new
        {
            job_id = jobId,
            status = JobStatus.Cancelled.ToString(),
            message = "Job cancellation requested",
            cancelled_at = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Retry a failed job
    /// </summary>
    [HttpPost("{jobId}/retry")]
    public async Task<ActionResult> RetryJob(string jobId)
    {
        var newJobId = Guid.NewGuid().ToString();

        return Ok(new
        {
            original_job_id = jobId,
            new_job_id = newJobId,
            status = JobStatus.Pending.ToString(),
            message = "Job retry scheduled",
            created_at = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get job statistics and metrics
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult> GetJobStats(
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var stats = new
        {
            total_jobs = 156,
            completed_jobs = 142,
            failed_jobs = 8,
            running_jobs = 3,
            pending_jobs = 3,
            success_rate = 91.0,
            average_processing_time_minutes = 12.5,
            job_types = new[] {
                new { type = "video_processing", count = 89, success_rate = 95.5 },
                new { type = "embedding_generation", count = 45, success_rate = 100.0 },
                new { type = "video_download", count = 22, success_rate = 72.7 }
            },
            daily_stats = new[] {
                new { date = DateTime.UtcNow.Date.AddDays(-6), completed = 15, failed = 1 },
                new { date = DateTime.UtcNow.Date.AddDays(-5), completed = 18, failed = 0 },
                new { date = DateTime.UtcNow.Date.AddDays(-4), completed = 22, failed = 2 },
                new { date = DateTime.UtcNow.Date.AddDays(-3), completed = 19, failed = 1 },
                new { date = DateTime.UtcNow.Date.AddDays(-2), completed = 25, failed = 1 },
                new { date = DateTime.UtcNow.Date.AddDays(-1), completed = 28, failed = 2 },
                new { date = DateTime.UtcNow.Date, completed = 15, failed = 1 }
            }
        };

        return Ok(stats);
    }

    /// <summary>
    /// Get system resource usage for job processing
    /// </summary>
    [HttpGet("resources")]
    public async Task<ActionResult> GetResourceUsage()
    {
        return Ok(new
        {
            cpu_usage_percent = 45.2,
            memory_usage_percent = 62.8,
            disk_usage_percent = 23.5,
            active_workers = 3,
            max_workers = 8,
            queue_size = 2,
            processing_capacity = new
            {
                videos_per_hour = 24,
                current_throughput = 18
            },
            system_health = "healthy"
        });
    }

    /// <summary>
    /// Create a new background job
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> CreateJob([FromBody] CreateJobRequest request)
    {
        if (string.IsNullOrEmpty(request.JobType))
        {
            return BadRequest(new { error = new { code = "VALIDATION_ERROR", message = "Job type is required" } });
        }

        var jobId = Guid.NewGuid().ToString();

        return Ok(new
        {
            id = jobId,
            job_type = request.JobType,
            status = JobStatus.Pending.ToString(),
            parameters = request.Parameters,
            created_at = DateTime.UtcNow,
            message = "Job created successfully"
        });
    }

    /// <summary>
    /// Bulk operations on jobs
    /// </summary>
    [HttpPost("bulk")]
    public async Task<ActionResult> BulkJobOperation([FromBody] BulkJobRequest request)
    {
        var results = request.JobIds.Select(jobId => new
        {
            job_id = jobId,
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
}

public class CreateJobRequest
{
    public string JobType { get; set; } = string.Empty;
    public string? VideoId { get; set; }
    public Dictionary<string, object>? Parameters { get; set; }
    public int Priority { get; set; } = 0;
}

public class BulkJobRequest
{
    public string[] JobIds { get; set; } = Array.Empty<string>();
    public string Operation { get; set; } = string.Empty; // cancel, retry, delete
}
