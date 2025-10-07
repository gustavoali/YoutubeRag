using YoutubeRag.Application.Interfaces;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;
using YoutubeRag.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace YoutubeRag.Infrastructure.Services;

public class JobService : IJobService
{
    private readonly ILogger<JobService> _logger;
    private readonly ApplicationDbContext _context;

    public JobService(ILogger<JobService> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<Job> CreateJobAsync(string jobType, string userId, string? videoId = null, Dictionary<string, object>? parameters = null)
    {
        try
        {
            var job = new Job
            {
                Id = Guid.NewGuid().ToString(),
                Type = Enum.Parse<JobType>(jobType),
                UserId = userId,
                VideoId = videoId,
                Status = JobStatus.Pending,
                Parameters = parameters != null ? JsonSerializer.Serialize(parameters) : null,
                Progress = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created job {JobId} of type {JobType} for user {UserId}",
                job.Id, jobType, userId);

            return job;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating job of type {JobType} for user {UserId}", jobType, userId);
            throw;
        }
    }

    public async Task<Job> UpdateJobStatusAsync(string jobId, JobStatus status, string? statusMessage = null, int? progress = null)
    {
        try
        {
            var job = await _context.Jobs.FindAsync(jobId);
            if (job == null)
                throw new ArgumentException($"Job not found: {jobId}");

            job.Status = status;
            job.UpdatedAt = DateTime.UtcNow;

            if (statusMessage != null)
                job.StatusMessage = statusMessage;

            if (progress.HasValue)
                job.Progress = Math.Clamp(progress.Value, 0, 100);

            if (status == JobStatus.Running && job.StartedAt == null)
                job.StartedAt = DateTime.UtcNow;

            if (status == JobStatus.Completed || status == JobStatus.Failed)
                job.CompletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogDebug("Updated job {JobId} status to {Status} with progress {Progress}%",
                jobId, status, job.Progress);

            return job;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating job status for job: {JobId}", jobId);
            throw;
        }
    }

    public async Task<Job?> GetJobAsync(string jobId)
    {
        try
        {
            return await _context.Jobs
                .Include(j => j.User)
                .Include(j => j.Video)
                .FirstOrDefaultAsync(j => j.Id == jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job: {JobId}", jobId);
            throw;
        }
    }

    public async Task<List<Job>> GetUserJobsAsync(string userId, int page = 1, int pageSize = 20)
    {
        try
        {
            return await _context.Jobs
                .Where(j => j.UserId == userId)
                .Include(j => j.Video)
                .OrderByDescending(j => j.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting jobs for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<List<Job>> GetActiveJobsAsync()
    {
        try
        {
            return await _context.Jobs
                .Where(j => j.Status == JobStatus.Pending || j.Status == JobStatus.Running)
                .Include(j => j.User)
                .Include(j => j.Video)
                .OrderBy(j => j.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active jobs");
            throw;
        }
    }

    public async Task<bool> CancelJobAsync(string jobId)
    {
        try
        {
            var job = await _context.Jobs.FindAsync(jobId);
            if (job == null)
                return false;

            if (job.Status == JobStatus.Pending || job.Status == JobStatus.Running)
            {
                job.Status = JobStatus.Failed;
                job.StatusMessage = "Job cancelled by user";
                job.CompletedAt = DateTime.UtcNow;
                job.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Cancelled job: {JobId}", jobId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling job: {JobId}", jobId);
            return false;
        }
    }

    public async Task<JobExecutionResult> ExecuteJobAsync(string jobId)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var job = await GetJobAsync(jobId);
            if (job == null)
                throw new ArgumentException($"Job not found: {jobId}");

            await UpdateJobStatusAsync(jobId, JobStatus.Running, "Job execution started");

            _logger.LogInformation("Starting execution of job {JobId} of type {JobType}", jobId, job.Type);

            // Simulate job execution based on job type
            var result = await ExecuteJobByTypeAsync(job);

            await UpdateJobStatusAsync(jobId, JobStatus.Completed, "Job completed successfully", 100);

            _logger.LogInformation("Successfully completed job {JobId} in {Duration}ms",
                jobId, (DateTime.UtcNow - startTime).TotalMilliseconds);

            return new JobExecutionResult
            {
                Success = true,
                Results = result,
                ExecutionTime = DateTime.UtcNow - startTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing job: {JobId}", jobId);

            await UpdateJobStatusAsync(jobId, JobStatus.Failed, $"Job failed: {ex.Message}");

            return new JobExecutionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ExecutionTime = DateTime.UtcNow - startTime
            };
        }
    }

    private async Task<Dictionary<string, object>> ExecuteJobByTypeAsync(Job job)
    {
        var results = new Dictionary<string, object>();

        switch (job.Type.ToString())
        {
            case "ProcessVideoFromUrl":
                results["message"] = "Video processing from URL initiated";
                results["videoId"] = job.VideoId ?? "";
                break;

            case "ProcessVideoFromFile":
                results["message"] = "Video processing from file initiated";
                results["videoId"] = job.VideoId ?? "";
                break;

            case "GenerateTranscript":
                results["message"] = "Transcript generation initiated";
                results["videoId"] = job.VideoId ?? "";
                break;

            case "GenerateEmbeddings":
                results["message"] = "Embeddings generation initiated";
                results["videoId"] = job.VideoId ?? "";
                break;

            default:
                throw new NotSupportedException($"Job type not supported: {job.Type}");
        }

        // Simulate some processing time
        await Task.Delay(100);

        return results;
    }
}