using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Tests.Unit.Builders.Entities;

/// <summary>
/// Builder for creating Job test instances
/// </summary>
public class JobBuilder
{
    private string _id = Guid.NewGuid().ToString();
    private JobType _type = JobType.VideoProcessing;
    private JobStatus _status = JobStatus.Pending;
    private string? _statusMessage = "Job created, waiting for background processing";
    private int _progress = 0;
    private PipelineStage _currentStage = PipelineStage.None;
    private string? _stageProgressJson = null;
    private string? _result = null;
    private string? _errorMessage = null;
    private string? _errorStackTrace = null;
    private string? _errorType = null;
    private PipelineStage? _failedStage = null;
    private string? _parameters = null;
    private string? _metadata = null;
    private DateTime? _startedAt = null;
    private DateTime? _completedAt = null;
    private DateTime? _failedAt = null;
    private int _retryCount = 0;
    private int _maxRetries = 3;
    private DateTime? _nextRetryAt = null;
    private string? _lastFailureCategory = null;
    private int _priority = 1;
    private string? _workerId = null;
    private string? _hangfireJobId = null;
    private string _userId = Guid.NewGuid().ToString();
    private string? _videoId = null;
    private DateTime _createdAt = DateTime.UtcNow;
    private DateTime _updatedAt = DateTime.UtcNow;

    public JobBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public JobBuilder WithType(JobType type)
    {
        _type = type;
        return this;
    }

    public JobBuilder WithStatus(JobStatus status)
    {
        _status = status;
        return this;
    }

    public JobBuilder WithStatusMessage(string? statusMessage)
    {
        _statusMessage = statusMessage;
        return this;
    }

    public JobBuilder WithProgress(int progress)
    {
        _progress = progress;
        return this;
    }

    public JobBuilder WithCurrentStage(PipelineStage stage)
    {
        _currentStage = stage;
        return this;
    }

    public JobBuilder WithStageProgressJson(string? stageProgressJson)
    {
        _stageProgressJson = stageProgressJson;
        return this;
    }

    public JobBuilder WithResult(string? result)
    {
        _result = result;
        return this;
    }

    public JobBuilder WithErrorMessage(string? errorMessage)
    {
        _errorMessage = errorMessage;
        return this;
    }

    public JobBuilder WithErrorStackTrace(string? errorStackTrace)
    {
        _errorStackTrace = errorStackTrace;
        return this;
    }

    public JobBuilder WithErrorType(string? errorType)
    {
        _errorType = errorType;
        return this;
    }

    public JobBuilder WithFailedStage(PipelineStage? failedStage)
    {
        _failedStage = failedStage;
        return this;
    }

    public JobBuilder WithParameters(string? parameters)
    {
        _parameters = parameters;
        return this;
    }

    public JobBuilder WithMetadata(string? metadata)
    {
        _metadata = metadata;
        return this;
    }

    public JobBuilder WithStartedAt(DateTime? startedAt)
    {
        _startedAt = startedAt;
        return this;
    }

    public JobBuilder WithCompletedAt(DateTime? completedAt)
    {
        _completedAt = completedAt;
        return this;
    }

    public JobBuilder WithFailedAt(DateTime? failedAt)
    {
        _failedAt = failedAt;
        return this;
    }

    public JobBuilder WithRetryCount(int retryCount)
    {
        _retryCount = retryCount;
        return this;
    }

    public JobBuilder WithMaxRetries(int maxRetries)
    {
        _maxRetries = maxRetries;
        return this;
    }

    public JobBuilder WithNextRetryAt(DateTime? nextRetryAt)
    {
        _nextRetryAt = nextRetryAt;
        return this;
    }

    public JobBuilder WithLastFailureCategory(string? lastFailureCategory)
    {
        _lastFailureCategory = lastFailureCategory;
        return this;
    }

    public JobBuilder WithPriority(int priority)
    {
        _priority = priority;
        return this;
    }

    public JobBuilder WithWorkerId(string? workerId)
    {
        _workerId = workerId;
        return this;
    }

    public JobBuilder WithHangfireJobId(string? hangfireJobId)
    {
        _hangfireJobId = hangfireJobId;
        return this;
    }

    public JobBuilder WithUserId(string userId)
    {
        _userId = userId;
        return this;
    }

    public JobBuilder WithVideoId(string? videoId)
    {
        _videoId = videoId;
        return this;
    }

    public JobBuilder WithCreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public JobBuilder WithUpdatedAt(DateTime updatedAt)
    {
        _updatedAt = updatedAt;
        return this;
    }

    public Job Build()
    {
        return new Job
        {
            Id = _id,
            Type = _type,
            Status = _status,
            StatusMessage = _statusMessage,
            Progress = _progress,
            CurrentStage = _currentStage,
            StageProgressJson = _stageProgressJson,
            Result = _result,
            ErrorMessage = _errorMessage,
            ErrorStackTrace = _errorStackTrace,
            ErrorType = _errorType,
            FailedStage = _failedStage,
            Parameters = _parameters,
            Metadata = _metadata,
            StartedAt = _startedAt,
            CompletedAt = _completedAt,
            FailedAt = _failedAt,
            RetryCount = _retryCount,
            MaxRetries = _maxRetries,
            NextRetryAt = _nextRetryAt,
            LastFailureCategory = _lastFailureCategory,
            Priority = _priority,
            WorkerId = _workerId,
            HangfireJobId = _hangfireJobId,
            UserId = _userId,
            VideoId = _videoId,
            CreatedAt = _createdAt,
            UpdatedAt = _updatedAt
        };
    }

    /// <summary>
    /// Creates a valid pending Job with default values for video processing
    /// </summary>
    public static Job CreatePendingVideoProcessing() => new JobBuilder().Build();

    /// <summary>
    /// Creates a completed Job
    /// </summary>
    public static Job CreateCompleted() =>
        new JobBuilder()
            .WithStatus(JobStatus.Completed)
            .WithProgress(100)
            .WithCurrentStage(PipelineStage.Segmentation)
            .WithCompletedAt(DateTime.UtcNow)
            .Build();

    /// <summary>
    /// Creates a failed Job
    /// </summary>
    public static Job CreateFailed() =>
        new JobBuilder()
            .WithStatus(JobStatus.Failed)
            .WithErrorMessage("Job failed")
            .WithFailedStage(PipelineStage.Download)
            .WithFailedAt(DateTime.UtcNow)
            .Build();

    /// <summary>
    /// Creates a running Job
    /// </summary>
    public static Job CreateRunning() =>
        new JobBuilder()
            .WithStatus(JobStatus.Running)
            .WithProgress(50)
            .WithCurrentStage(PipelineStage.Transcription)
            .WithStartedAt(DateTime.UtcNow)
            .Build();

    /// <summary>
    /// Creates a Job with retry count
    /// </summary>
    public static Job CreateWithRetry(int retryCount) =>
        new JobBuilder()
            .WithRetryCount(retryCount)
            .WithNextRetryAt(DateTime.UtcNow.AddMinutes(5))
            .Build();
}
