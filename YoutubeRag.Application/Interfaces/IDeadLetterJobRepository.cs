using YoutubeRag.Domain.Entities;

namespace YoutubeRag.Application.Interfaces;

/// <summary>
/// Repository interface for Dead Letter Queue operations
/// </summary>
public interface IDeadLetterJobRepository : IRepository<DeadLetterJob>
{
    /// <summary>
    /// Gets all dead letter jobs ordered by failure date (most recent first)
    /// </summary>
    /// <param name="limit">Maximum number of entries to return</param>
    /// <returns>Collection of dead letter jobs</returns>
    Task<IEnumerable<DeadLetterJob>> GetAllAsync(int limit = 100);

    /// <summary>
    /// Gets a dead letter job by the original job ID
    /// </summary>
    /// <param name="jobId">The original job ID</param>
    /// <returns>The dead letter job if found; otherwise, null</returns>
    Task<DeadLetterJob?> GetByJobIdAsync(string jobId);

    /// <summary>
    /// Gets dead letter jobs with their related job data
    /// </summary>
    /// <param name="includeRequeued">Whether to include requeued jobs in results</param>
    /// <param name="limit">Maximum number of entries to return</param>
    /// <returns>Collection of dead letter jobs with related data</returns>
    Task<IEnumerable<DeadLetterJob>> GetWithRelatedDataAsync(bool includeRequeued = false, int limit = 100);

    /// <summary>
    /// Gets dead letter jobs by failure reason
    /// </summary>
    /// <param name="failureReason">The failure reason to filter by</param>
    /// <returns>Collection of matching dead letter jobs</returns>
    Task<IEnumerable<DeadLetterJob>> GetByFailureReasonAsync(string failureReason);

    /// <summary>
    /// Marks a dead letter job as requeued
    /// </summary>
    /// <param name="deadLetterJobId">The dead letter job ID</param>
    /// <param name="requeuedBy">User or system that triggered the requeue</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful; otherwise, false</returns>
    Task<bool> MarkAsRequeuedAsync(string deadLetterJobId, string requeuedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets dead letter jobs created within a specific date range
    /// </summary>
    /// <param name="startDate">Start date of the range</param>
    /// <param name="endDate">End date of the range</param>
    /// <returns>Collection of dead letter jobs within the date range</returns>
    Task<IEnumerable<DeadLetterJob>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets count of dead letter jobs by failure reason for monitoring
    /// </summary>
    /// <returns>Dictionary of failure reasons and their counts</returns>
    Task<Dictionary<string, int>> GetFailureReasonStatisticsAsync();
}
