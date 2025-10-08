using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Application.Interfaces;

/// <summary>
/// Repository interface for Job entity operations
/// </summary>
public interface IJobRepository : IRepository<Job>
{
    /// <summary>
    /// Gets all jobs for a specific user
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <returns>A collection of jobs belonging to the user</returns>
    Task<IEnumerable<Job>> GetByUserIdAsync(string userId);

    /// <summary>
    /// Gets all jobs for a specific video
    /// </summary>
    /// <param name="videoId">The video's unique identifier</param>
    /// <returns>A collection of jobs associated with the video</returns>
    Task<IEnumerable<Job>> GetByVideoIdAsync(string videoId);

    /// <summary>
    /// Gets jobs by their processing status
    /// </summary>
    /// <param name="status">The job processing status</param>
    /// <returns>A collection of jobs with the specified status</returns>
    Task<IEnumerable<Job>> GetByStatusAsync(JobStatus status);

    /// <summary>
    /// Gets jobs for a user filtered by status
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <param name="status">The job processing status</param>
    /// <returns>A collection of jobs matching the criteria</returns>
    Task<IEnumerable<Job>> GetByUserIdAndStatusAsync(string userId, JobStatus status);

    /// <summary>
    /// Gets jobs by type
    /// </summary>
    /// <param name="jobType">The type of job</param>
    /// <returns>A collection of jobs of the specified type</returns>
    Task<IEnumerable<Job>> GetByJobTypeAsync(string jobType);

    /// <summary>
    /// Gets a job with its related user and video data
    /// </summary>
    /// <param name="jobId">The job's unique identifier</param>
    /// <returns>The job with loaded related data if found; otherwise, null</returns>
    Task<Job?> GetWithRelatedDataAsync(string jobId);

    /// <summary>
    /// Gets pending jobs ordered by creation date
    /// </summary>
    /// <param name="limit">Maximum number of jobs to retrieve</param>
    /// <returns>A collection of pending jobs</returns>
    Task<IEnumerable<Job>> GetPendingJobsAsync(int limit = 10);

    /// <summary>
    /// Gets failed jobs that can be retried
    /// </summary>
    /// <param name="maxRetryCount">Maximum retry count to consider</param>
    /// <returns>A collection of failed jobs that can be retried</returns>
    Task<IEnumerable<Job>> GetRetriableFailedJobsAsync(int maxRetryCount = 3);

    /// <summary>
    /// Gets jobs that have been running longer than expected
    /// </summary>
    /// <param name="timeoutMinutes">The timeout period in minutes</param>
    /// <returns>A collection of potentially stuck jobs</returns>
    Task<IEnumerable<Job>> GetStuckJobsAsync(int timeoutMinutes = 30);

    /// <summary>
    /// Gets the latest job for a video
    /// </summary>
    /// <param name="videoId">The video's unique identifier</param>
    /// <returns>The most recent job for the video if found; otherwise, null</returns>
    Task<Job?> GetLatestByVideoIdAsync(string videoId);

    /// <summary>
    /// Gets jobs created within a specific date range
    /// </summary>
    /// <param name="startDate">The start date of the range</param>
    /// <param name="endDate">The end date of the range</param>
    /// <returns>A collection of jobs created within the date range</returns>
    Task<IEnumerable<Job>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Checks if there's an active job for a video
    /// </summary>
    /// <param name="videoId">The video's unique identifier</param>
    /// <returns>True if there's an active job; otherwise, false</returns>
    Task<bool> HasActiveJobForVideoAsync(string videoId);
}