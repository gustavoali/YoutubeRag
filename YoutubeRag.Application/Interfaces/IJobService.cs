using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Application.Interfaces;

public interface IJobService
{
    Task<Job> CreateJobAsync(string jobType, string userId, string? videoId = null, Dictionary<string, object>? parameters = null);
    Task<Job> UpdateJobStatusAsync(string jobId, JobStatus status, string? statusMessage = null, int? progress = null);
    Task<Job?> GetJobAsync(string jobId);
    Task<List<Job>> GetUserJobsAsync(string userId, int page = 1, int pageSize = 20);
    Task<List<Job>> GetActiveJobsAsync();
    Task<bool> CancelJobAsync(string jobId);
    Task<JobExecutionResult> ExecuteJobAsync(string jobId);
}

public class JobExecutionResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Results { get; set; } = new();
    public TimeSpan ExecutionTime { get; set; }
}
