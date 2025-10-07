using YoutubeRag.Application.Interfaces;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace YoutubeRag.Infrastructure.Services;

public class MockJobService : IJobService
{
    private readonly ILogger<MockJobService> _logger;
    private readonly List<Job> _mockJobs = new();

    public MockJobService(ILogger<MockJobService> logger)
    {
        _logger = logger;
    }

    public async Task<Job> CreateJobAsync(string jobType, string userId, string? videoId = null, Dictionary<string, object>? parameters = null)
    {
        _logger.LogInformation("Mock: Creating job of type {JobType} for user {UserId}", jobType, userId);

        await Task.Delay(100); // Simulate database operation

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            Type = Enum.Parse<JobType>(jobType),
            UserId = userId,
            VideoId = videoId,
            Status = JobStatus.Pending,
            Parameters = parameters != null ? System.Text.Json.JsonSerializer.Serialize(parameters) : null,
            Progress = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockJobs.Add(job);

        // Simulate job processing
        _ = Task.Run(async () =>
        {
            await Task.Delay(1000);
            await UpdateJobStatusAsync(job.Id, JobStatus.Running, "Job execution started");

            await Task.Delay(3000);
            await UpdateJobStatusAsync(job.Id, JobStatus.Completed, "Job completed successfully", 100);
        });

        return job;
    }

    public async Task<Job> UpdateJobStatusAsync(string jobId, JobStatus status, string? statusMessage = null, int? progress = null)
    {
        _logger.LogDebug("Mock: Updating job {JobId} status to {Status}", jobId, status);

        await Task.Delay(50); // Simulate database operation

        var job = _mockJobs.FirstOrDefault(j => j.Id == jobId);
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

        return job;
    }

    public async Task<Job?> GetJobAsync(string jobId)
    {
        await Task.Delay(50); // Simulate database query

        return _mockJobs.FirstOrDefault(j => j.Id == jobId);
    }

    public async Task<List<Job>> GetUserJobsAsync(string userId, int page = 1, int pageSize = 20)
    {
        _logger.LogDebug("Mock: Getting jobs for user {UserId}, page {Page}", userId, page);

        await Task.Delay(100); // Simulate database query

        return _mockJobs
            .Where(j => j.UserId == userId)
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public async Task<List<Job>> GetActiveJobsAsync()
    {
        await Task.Delay(100); // Simulate database query

        return _mockJobs
            .Where(j => j.Status == JobStatus.Pending || j.Status == JobStatus.Running)
            .OrderBy(j => j.CreatedAt)
            .ToList();
    }

    public async Task<bool> CancelJobAsync(string jobId)
    {
        _logger.LogInformation("Mock: Cancelling job: {JobId}", jobId);

        await Task.Delay(100); // Simulate cancellation operation

        var job = _mockJobs.FirstOrDefault(j => j.Id == jobId);
        if (job == null)
            return false;

        if (job.Status == JobStatus.Pending || job.Status == JobStatus.Running)
        {
            await UpdateJobStatusAsync(jobId, JobStatus.Failed, "Job cancelled by user");
            return true;
        }

        return false;
    }

    public async Task<JobExecutionResult> ExecuteJobAsync(string jobId)
    {
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("Mock: Executing job: {JobId}", jobId);

        try
        {
            var job = await GetJobAsync(jobId);
            if (job == null)
                throw new ArgumentException($"Job not found: {jobId}");

            await UpdateJobStatusAsync(jobId, JobStatus.Running, "Mock job execution started");

            // Simulate job execution time
            await Task.Delay(2000);

            await UpdateJobStatusAsync(jobId, JobStatus.Completed, "Mock job completed successfully", 100);

            var results = new Dictionary<string, object>
            {
                ["message"] = $"Mock execution of {job.Type} completed",
                ["videoId"] = job.VideoId ?? "",
                ["executionTime"] = (DateTime.UtcNow - startTime).TotalMilliseconds
            };

            return new JobExecutionResult
            {
                Success = true,
                Results = results,
                ExecutionTime = DateTime.UtcNow - startTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mock: Error executing job: {JobId}", jobId);

            await UpdateJobStatusAsync(jobId, JobStatus.Failed, $"Mock job failed: {ex.Message}");

            return new JobExecutionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ExecutionTime = DateTime.UtcNow - startTime
            };
        }
    }
}