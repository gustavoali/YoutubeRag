using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Infrastructure.Data;

namespace YoutubeRag.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Dead Letter Queue operations
/// </summary>
public class DeadLetterJobRepository : Repository<DeadLetterJob>, IDeadLetterJobRepository
{
    /// <summary>
    /// Initializes a new instance of the DeadLetterJobRepository class
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="logger">The logger instance</param>
    public DeadLetterJobRepository(ApplicationDbContext context, ILogger<DeadLetterJobRepository> logger)
        : base(context, logger)
    {
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DeadLetterJob>> GetAllAsync(int limit = 100)
    {
        try
        {
            return await _dbSet
                .OrderByDescending(dlj => dlj.FailedAt)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dead letter jobs with limit {Limit}", limit);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DeadLetterJob?> GetByJobIdAsync(string jobId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);

        try
        {
            return await _dbSet
                .FirstOrDefaultAsync(dlj => dlj.JobId == jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dead letter job for job ID {JobId}", jobId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DeadLetterJob>> GetWithRelatedDataAsync(bool includeRequeued = false, int limit = 100)
    {
        try
        {
            var query = _dbSet
                .Include(dlj => dlj.Job)
                    .ThenInclude(j => j.Video)
                .Include(dlj => dlj.Job)
                    .ThenInclude(j => j.User)
                .AsQueryable();

            if (!includeRequeued)
            {
                query = query.Where(dlj => !dlj.IsRequeued);
            }

            return await query
                .OrderByDescending(dlj => dlj.FailedAt)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dead letter jobs with related data. IncludeRequeued: {IncludeRequeued}, Limit: {Limit}",
                includeRequeued, limit);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DeadLetterJob>> GetByFailureReasonAsync(string failureReason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(failureReason);

        try
        {
            return await _dbSet
                .Where(dlj => dlj.FailureReason == failureReason)
                .OrderByDescending(dlj => dlj.FailedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dead letter jobs by failure reason {FailureReason}", failureReason);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> MarkAsRequeuedAsync(string deadLetterJobId, string requeuedBy, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deadLetterJobId);
        ArgumentException.ThrowIfNullOrWhiteSpace(requeuedBy);

        try
        {
            var deadLetterJob = await _dbSet.FindAsync(new object[] { deadLetterJobId }, cancellationToken);
            if (deadLetterJob == null)
            {
                _logger.LogWarning("Dead letter job {DeadLetterJobId} not found for requeue operation", deadLetterJobId);
                return false;
            }

            if (deadLetterJob.IsRequeued)
            {
                _logger.LogWarning("Dead letter job {DeadLetterJobId} has already been requeued", deadLetterJobId);
                return false;
            }

            deadLetterJob.IsRequeued = true;
            deadLetterJob.RequeuedAt = DateTime.UtcNow;
            deadLetterJob.RequeuedBy = requeuedBy;
            deadLetterJob.UpdatedAt = DateTime.UtcNow;

            await UpdateAsync(deadLetterJob);

            _logger.LogInformation("Marked dead letter job {DeadLetterJobId} as requeued by {RequeuedBy}",
                deadLetterJobId, requeuedBy);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking dead letter job {DeadLetterJobId} as requeued", deadLetterJobId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DeadLetterJob>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
        {
            throw new ArgumentException("Start date must be before or equal to end date");
        }

        try
        {
            return await _dbSet
                .Where(dlj => dlj.FailedAt >= startDate && dlj.FailedAt <= endDate)
                .OrderByDescending(dlj => dlj.FailedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dead letter jobs by date range {StartDate} - {EndDate}",
                startDate, endDate);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, int>> GetFailureReasonStatisticsAsync()
    {
        try
        {
            return await _dbSet
                .GroupBy(dlj => dlj.FailureReason)
                .Select(g => new { FailureReason = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.FailureReason, x => x.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving failure reason statistics");
            throw;
        }
    }
}
