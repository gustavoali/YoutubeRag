using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;
using YoutubeRag.Infrastructure.Data;

namespace YoutubeRag.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Job entity operations
/// </summary>
public class JobRepository : Repository<Job>, IJobRepository
{
    /// <summary>
    /// Initializes a new instance of the JobRepository class
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="logger">The logger instance</param>
    public JobRepository(ApplicationDbContext context, ILogger<JobRepository> logger)
        : base(context, logger)
    {
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Job>> GetByUserIdAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        try
        {
            return await _dbSet
                .Where(j => j.UserId == userId)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving jobs for user ID {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Job>> GetByVideoIdAsync(string videoId)
    {
        if (string.IsNullOrWhiteSpace(videoId))
        {
            throw new ArgumentException("Video ID cannot be null or empty", nameof(videoId));
        }

        try
        {
            return await _dbSet
                .Where(j => j.VideoId == videoId)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving jobs for video ID {VideoId}", videoId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Job>> GetByStatusAsync(JobStatus status)
    {
        try
        {
            return await _dbSet
                .Where(j => j.Status == status)
                .OrderBy(j => j.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving jobs by status {Status}", status);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Job>> GetByUserIdAndStatusAsync(string userId, JobStatus status)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        try
        {
            return await _dbSet
                .Where(j => j.UserId == userId && j.Status == status)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving jobs for user ID {UserId} with status {Status}", userId, status);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Job>> GetByJobTypeAsync(string jobType)
    {
        if (string.IsNullOrWhiteSpace(jobType))
        {
            throw new ArgumentException("Job type cannot be null or empty", nameof(jobType));
        }

        try
        {
            return await _dbSet
                .Where(j => j.Type.ToString() == jobType)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving jobs by type {JobType}", jobType);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Job?> GetWithRelatedDataAsync(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new ArgumentException("Job ID cannot be null or empty", nameof(jobId));
        }

        try
        {
            return await _dbSet
                .Include(j => j.User)
                .Include(j => j.Video)
                .FirstOrDefaultAsync(j => j.Id == jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job with related data for job ID {JobId}", jobId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Job>> GetPendingJobsAsync(int limit = 10)
    {
        if (limit < 1)
        {
            throw new ArgumentException("Limit must be greater than 0", nameof(limit));
        }

        try
        {
            return await _dbSet
                .Where(j => j.Status == JobStatus.Pending)
                .OrderBy(j => j.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending jobs with limit {Limit}", limit);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Job>> GetRetriableFailedJobsAsync(int maxRetryCount = 3)
    {
        if (maxRetryCount < 0)
        {
            throw new ArgumentException("Max retry count cannot be negative", nameof(maxRetryCount));
        }

        try
        {
            return await _dbSet
                .Where(j => j.Status == JobStatus.Failed && j.RetryCount < maxRetryCount)
                .OrderBy(j => j.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving retriable failed jobs with max retry count {MaxRetryCount}", maxRetryCount);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Job>> GetStuckJobsAsync(int timeoutMinutes = 30)
    {
        if (timeoutMinutes < 1)
        {
            throw new ArgumentException("Timeout minutes must be greater than 0", nameof(timeoutMinutes));
        }

        try
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-timeoutMinutes);
            return await _dbSet
                .Where(j => j.Status == JobStatus.Running &&
                           j.StartedAt != null &&
                           j.StartedAt < cutoffTime)
                .OrderBy(j => j.StartedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stuck jobs with timeout {TimeoutMinutes} minutes", timeoutMinutes);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Job?> GetLatestByVideoIdAsync(string videoId)
    {
        if (string.IsNullOrWhiteSpace(videoId))
        {
            throw new ArgumentException("Video ID cannot be null or empty", nameof(videoId));
        }

        try
        {
            return await _dbSet
                .Where(j => j.VideoId == videoId)
                .OrderByDescending(j => j.CreatedAt)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving latest job for video ID {VideoId}", videoId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Job>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        if (endDate < startDate)
        {
            throw new ArgumentException("End date must be after start date", nameof(endDate));
        }

        try
        {
            return await _dbSet
                .Where(j => j.CreatedAt >= startDate && j.CreatedAt <= endDate)
                .OrderBy(j => j.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving jobs between {StartDate} and {EndDate}", startDate, endDate);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> HasActiveJobForVideoAsync(string videoId)
    {
        if (string.IsNullOrWhiteSpace(videoId))
        {
            throw new ArgumentException("Video ID cannot be null or empty", nameof(videoId));
        }

        try
        {
            return await _dbSet
                .AnyAsync(j => j.VideoId == videoId &&
                              (j.Status == JobStatus.Pending || j.Status == JobStatus.Running));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for active job for video ID {VideoId}", videoId);
            throw;
        }
    }
}