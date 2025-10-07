# Hangfire Implementation Guide for Video Processing Pipeline

## Installation and Configuration

### 1. Required NuGet Packages

Add these packages to `YoutubeRag.Api.csproj`:

```xml
<PackageReference Include="Hangfire.Core" Version="1.8.6" />
<PackageReference Include="Hangfire.AspNetCore" Version="1.8.6" />
<PackageReference Include="Hangfire.MySqlConnector" Version="0.9.5" />
<PackageReference Include="Hangfire.Dashboard.Authorization" Version="3.0.1" />
```

Add to `YoutubeRag.Infrastructure.csproj`:

```xml
<PackageReference Include="Hangfire.Core" Version="1.8.6" />
```

### 2. Hangfire Configuration in Program.cs

```csharp
using Hangfire;
using Hangfire.MySql;
using Hangfire.Dashboard;

// Add Hangfire services
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseStorage(
        new MySqlStorage(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            new MySqlStorageOptions
            {
                TablesPrefix = "Hangfire_",
                QueuePollInterval = TimeSpan.FromSeconds(2),
                JobExpirationCheckInterval = TimeSpan.FromHours(1),
                CountersAggregateInterval = TimeSpan.FromMinutes(5),
                PrepareSchemaIfNecessary = true,
                DashboardJobListLimit = 50000,
                TransactionTimeout = TimeSpan.FromMinutes(1),
            }
        )
    )
    .UseFilter(new AutomaticRetryAttribute
    {
        Attempts = 3,
        DelaysInSeconds = new int[] { 10, 30, 60 },
        OnAttemptsExceeded = AttemptsExceededAction.Fail
    }));

// Add Hangfire server with multiple queues
builder.Services.AddHangfireServer(options =>
{
    options.ServerName = $"{Environment.MachineName}:{Guid.NewGuid().ToString("N").Substring(0, 8)}";
    options.WorkerCount = Environment.ProcessorCount * 2;
    options.Queues = new[] { "critical", "default", "low" };
    options.ShutdownTimeout = TimeSpan.FromMinutes(1);
    options.StopTimeout = TimeSpan.FromSeconds(30);
});

// Configure authorization for Hangfire Dashboard
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("HangfireAdmin", policy =>
        policy.RequireRole("Admin"));
});

// ... other service configurations ...

var app = builder.Build();

// Configure Hangfire Dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() },
    DashboardTitle = "YouTube RAG - Job Processing Dashboard",
    StatsPollingInterval = 2000,
    DisplayStorageConnectionString = false
});

// Map Hangfire endpoints
app.MapHangfireDashboard();
```

### 3. Custom Authorization Filter

```csharp
// YoutubeRag.Api/Filters/HangfireAuthorizationFilter.cs
using Hangfire.Dashboard;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Allow all in development
        if (httpContext.RequestServices
            .GetRequiredService<IWebHostEnvironment>()
            .IsDevelopment())
        {
            return true;
        }

        // In production, require admin role
        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.IsInRole("Admin");
    }
}
```

## Background Job Service Implementation

### 4. IBackgroundJobService Implementation

```csharp
// YoutubeRag.Infrastructure/Services/HangfireBackgroundJobService.cs
using Hangfire;
using YoutubeRag.Application.Interfaces;

public class HangfireBackgroundJobService : IBackgroundJobService
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly ILogger<HangfireBackgroundJobService> _logger;

    public HangfireBackgroundJobService(
        IBackgroundJobClient backgroundJobClient,
        IRecurringJobManager recurringJobManager,
        ILogger<HangfireBackgroundJobService> logger)
    {
        _backgroundJobClient = backgroundJobClient;
        _recurringJobManager = recurringJobManager;
        _logger = logger;
    }

    public string EnqueueJob<T>(
        Expression<Func<T, Task>> methodCall,
        JobPriority priority = JobPriority.Default)
    {
        var queue = MapPriorityToQueue(priority);
        var jobId = _backgroundJobClient.Enqueue(methodCall, queue);

        _logger.LogInformation(
            "Enqueued job {JobId} to queue {Queue}",
            jobId, queue);

        return jobId;
    }

    public string ScheduleJob<T>(
        Expression<Func<T, Task>> methodCall,
        TimeSpan delay)
    {
        var jobId = _backgroundJobClient.Schedule(methodCall, delay);

        _logger.LogInformation(
            "Scheduled job {JobId} to run after {Delay}",
            jobId, delay);

        return jobId;
    }

    public bool CancelJob(string jobId)
    {
        var result = _backgroundJobClient.Delete(jobId);

        _logger.LogInformation(
            "Cancelled job {JobId}: {Result}",
            jobId, result);

        return result;
    }

    public JobStatus GetJobStatus(string jobId)
    {
        var jobData = JobStorage.Current.GetConnection().GetJobData(jobId);

        if (jobData == null)
            return JobStatus.NotFound;

        return jobData.State switch
        {
            "Enqueued" => JobStatus.Pending,
            "Processing" => JobStatus.Running,
            "Succeeded" => JobStatus.Completed,
            "Failed" => JobStatus.Failed,
            "Deleted" => JobStatus.Cancelled,
            "Scheduled" => JobStatus.Scheduled,
            _ => JobStatus.Unknown
        };
    }

    public void ConfigureRecurringJob(
        string jobId,
        Expression<Func<Task>> methodCall,
        string cronExpression)
    {
        _recurringJobManager.AddOrUpdate(
            jobId,
            methodCall,
            cronExpression,
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Local
            });

        _logger.LogInformation(
            "Configured recurring job {JobId} with cron {Cron}",
            jobId, cronExpression);
    }

    private static string MapPriorityToQueue(JobPriority priority) =>
        priority switch
        {
            JobPriority.Critical => "critical",
            JobPriority.High => "default",
            JobPriority.Low => "low",
            _ => "default"
        };
}

public enum JobPriority
{
    Low = 0,
    Default = 1,
    High = 2,
    Critical = 3
}

public enum JobStatus
{
    NotFound,
    Pending,
    Scheduled,
    Running,
    Completed,
    Failed,
    Cancelled,
    Unknown
}
```

### 5. Video Processing Job Handler

```csharp
// YoutubeRag.Infrastructure/Jobs/VideoIngestionJobHandler.cs
using Hangfire;
using YoutubeRag.Application.Interfaces;

public class VideoIngestionJobHandler
{
    private readonly IJobOrchestrationService _orchestrationService;
    private readonly IVideoIngestionService _ingestionService;
    private readonly IProgressNotificationService _notificationService;
    private readonly IJobRepository _jobRepository;
    private readonly ILogger<VideoIngestionJobHandler> _logger;

    public VideoIngestionJobHandler(
        IJobOrchestrationService orchestrationService,
        IVideoIngestionService ingestionService,
        IProgressNotificationService notificationService,
        IJobRepository jobRepository,
        ILogger<VideoIngestionJobHandler> logger)
    {
        _orchestrationService = orchestrationService;
        _ingestionService = ingestionService;
        _notificationService = notificationService;
        _jobRepository = jobRepository;
        _logger = logger;
    }

    [JobDisplayName("Process Video: {0}")]
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 10, 30, 60 })]
    public async Task ProcessVideoAsync(
        string jobId,
        VideoIngestionRequest request,
        PerformContext context)
    {
        _logger.LogInformation(
            "Starting video processing job {JobId} for URL: {Url}",
            jobId, request.YouTubeUrl);

        try
        {
            // Update job status
            await _jobRepository.UpdateStatusAsync(
                jobId,
                JobStatus.Running,
                "Starting video processing pipeline");

            // Set Hangfire job ID for tracking
            context.SetJobParameter("DatabaseJobId", jobId);

            // Create pipeline
            var pipeline = new VideoIngestionPipeline();

            // Register progress callback
            await _orchestrationService.RegisterPipelineCallbackAsync(
                jobId,
                async (pipelineEvent) =>
                {
                    await HandlePipelineEventAsync(jobId, pipelineEvent, context);
                });

            // Execute pipeline
            var result = await _orchestrationService.ExecutePipelineAsync<VideoIngestionRequest, VideoIngestionResult>(
                pipeline,
                request,
                jobId);

            if (result.Success)
            {
                await HandleSuccessAsync(jobId, result.Result!, context);
            }
            else
            {
                await HandleFailureAsync(jobId, result.ErrorMessage!, context);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception in video processing job {JobId}",
                jobId);

            await HandleFailureAsync(jobId, ex.Message, context);
            throw; // Let Hangfire handle retry
        }
    }

    private async Task HandlePipelineEventAsync(
        string jobId,
        PipelineEvent pipelineEvent,
        PerformContext context)
    {
        // Update Hangfire progress
        var progress = CalculateOverallProgress(pipelineEvent);
        context.WriteProgressBar(progress);

        // Send real-time notification
        await _notificationService.NotifyProgressAsync(new ProgressNotification
        {
            JobId = jobId,
            Status = MapEventToJobStatus(pipelineEvent.Type),
            OverallProgress = progress,
            CurrentStage = pipelineEvent.StageName,
            Message = pipelineEvent.Message,
            Timestamp = pipelineEvent.Timestamp
        });

        // Log stage transitions
        if (pipelineEvent.Type == PipelineEventType.StageStarted)
        {
            _logger.LogInformation(
                "Job {JobId}: Starting stage {Stage}",
                jobId, pipelineEvent.StageName);
        }
        else if (pipelineEvent.Type == PipelineEventType.StageCompleted)
        {
            _logger.LogInformation(
                "Job {JobId}: Completed stage {Stage}",
                jobId, pipelineEvent.StageName);
        }
    }

    private async Task HandleSuccessAsync(
        string jobId,
        VideoIngestionResult result,
        PerformContext context)
    {
        await _jobRepository.UpdateStatusAsync(
            jobId,
            JobStatus.Completed,
            "Video processing completed successfully");

        await _notificationService.NotifyCompletionAsync(new CompletionNotification
        {
            JobId = jobId,
            VideoId = result.VideoId,
            VideoTitle = result.VideoTitle,
            CompletedAt = DateTime.UtcNow,
            ProcessingTime = TimeSpan.Parse(context.BackgroundJob.CreatedAt.ToString()),
            Stats = new JobCompletionStats
            {
                TotalSegments = result.SegmentCount,
                TotalEmbeddings = result.EmbeddingCount,
                VideoDuration = result.VideoDuration
            }
        });

        _logger.LogInformation(
            "Successfully completed video processing job {JobId}",
            jobId);
    }

    private async Task HandleFailureAsync(
        string jobId,
        string errorMessage,
        PerformContext context)
    {
        var canRetry = context.BackgroundJob.RetryCount < 3;

        await _jobRepository.UpdateStatusAsync(
            jobId,
            canRetry ? JobStatus.Retrying : JobStatus.Failed,
            errorMessage);

        await _notificationService.NotifyFailureAsync(new FailureNotification
        {
            JobId = jobId,
            ErrorMessage = errorMessage,
            FailedAt = DateTime.UtcNow,
            CanRetry = canRetry,
            RetryCount = context.BackgroundJob.RetryCount
        });

        _logger.LogError(
            "Failed video processing job {JobId}: {Error}",
            jobId, errorMessage);
    }

    private int CalculateOverallProgress(PipelineEvent pipelineEvent)
    {
        // Implementation to calculate overall progress based on stage weights
        return 0; // Placeholder
    }

    private JobStatus MapEventToJobStatus(PipelineEventType eventType) =>
        eventType switch
        {
            PipelineEventType.Started => JobStatus.Running,
            PipelineEventType.Completed => JobStatus.Completed,
            PipelineEventType.Failed => JobStatus.Failed,
            PipelineEventType.Cancelled => JobStatus.Cancelled,
            _ => JobStatus.Running
        };
}
```

## Usage Examples

### 6. Enqueuing a Video Processing Job

```csharp
// YoutubeRag.Api/Controllers/VideosController.cs
[ApiController]
[Route("api/[controller]")]
public class VideosController : ControllerBase
{
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly IJobService _jobService;

    [HttpPost("process")]
    public async Task<ActionResult<ProcessVideoResponse>> ProcessVideo(
        [FromBody] ProcessVideoRequest request)
    {
        // Create job record in database
        var job = await _jobService.CreateJobAsync(
            "VideoIngestion",
            User.GetUserId(),
            parameters: new Dictionary<string, object>
            {
                ["url"] = request.YouTubeUrl,
                ["mode"] = request.ProcessingMode
            });

        // Enqueue background job
        var hangfireJobId = _backgroundJobService.EnqueueJob<VideoIngestionJobHandler>(
            handler => handler.ProcessVideoAsync(
                job.Id,
                new VideoIngestionRequest
                {
                    YouTubeUrl = request.YouTubeUrl,
                    UserId = User.GetUserId(),
                    ProcessingMode = request.ProcessingMode,
                    Options = request.Options
                },
                null!), // PerformContext injected by Hangfire
            request.Priority);

        // Store Hangfire job ID for tracking
        await _jobService.UpdateJobMetadataAsync(
            job.Id,
            new { HangfireJobId = hangfireJobId });

        return Ok(new ProcessVideoResponse
        {
            JobId = job.Id,
            HangfireJobId = hangfireJobId,
            Status = "Queued",
            Message = "Video processing job has been queued"
        });
    }

    [HttpDelete("jobs/{jobId}")]
    public async Task<ActionResult> CancelJob(string jobId)
    {
        var job = await _jobService.GetJobAsync(jobId);
        if (job == null)
            return NotFound();

        // Get Hangfire job ID from metadata
        var metadata = JsonSerializer.Deserialize<JobMetadata>(job.Metadata);
        if (metadata?.HangfireJobId != null)
        {
            _backgroundJobService.CancelJob(metadata.HangfireJobId);
        }

        await _jobService.UpdateJobStatusAsync(
            jobId,
            JobStatus.Cancelled,
            "Cancelled by user");

        return NoContent();
    }
}
```

### 7. Monitoring Jobs

```csharp
// YoutubeRag.Api/Controllers/JobsController.cs
[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IJobService _jobService;
    private readonly IBackgroundJobService _backgroundJobService;

    [HttpGet("{jobId}/status")]
    public async Task<ActionResult<JobStatusResponse>> GetJobStatus(string jobId)
    {
        var job = await _jobService.GetJobAsync(jobId);
        if (job == null)
            return NotFound();

        // Get Hangfire status
        var metadata = JsonSerializer.Deserialize<JobMetadata>(job.Metadata);
        var hangfireStatus = metadata?.HangfireJobId != null
            ? _backgroundJobService.GetJobStatus(metadata.HangfireJobId)
            : JobStatus.Unknown;

        return Ok(new JobStatusResponse
        {
            JobId = job.Id,
            Status = job.Status,
            HangfireStatus = hangfireStatus,
            Progress = job.Progress,
            CurrentStage = job.StatusMessage,
            StartedAt = job.StartedAt,
            EstimatedCompletion = CalculateEstimatedCompletion(job),
            Stages = await GetStageDetails(job.Id)
        });
    }

    [HttpGet("active")]
    public async Task<ActionResult<List<ActiveJobDto>>> GetActiveJobs()
    {
        var jobs = await _jobService.GetActiveJobsAsync();

        var activeJobs = jobs.Select(job => new ActiveJobDto
        {
            JobId = job.Id,
            JobType = job.JobType,
            Status = job.Status,
            Progress = job.Progress,
            StartedAt = job.StartedAt,
            UserId = job.UserId,
            VideoTitle = job.Video?.Title
        }).ToList();

        return Ok(activeJobs);
    }
}
```

## Deployment Considerations

### 8. Scaling Hangfire Servers

For production deployment with multiple servers:

```csharp
// appsettings.Production.json
{
  "Hangfire": {
    "ServerCount": 3,
    "WorkerMultiplier": 2,
    "Queues": {
      "Critical": {
        "WorkerCount": 4,
        "Priority": 100
      },
      "Default": {
        "WorkerCount": 8,
        "Priority": 50
      },
      "Low": {
        "WorkerCount": 2,
        "Priority": 10
      }
    }
  }
}
```

### 9. Docker Deployment

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Install ffmpeg for video processing
RUN apt-get update && apt-get install -y ffmpeg

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["YoutubeRag.Api/YoutubeRag.Api.csproj", "YoutubeRag.Api/"]
# ... other project references ...
RUN dotnet restore "YoutubeRag.Api/YoutubeRag.Api.csproj"
COPY . .
WORKDIR "/src/YoutubeRag.Api"
RUN dotnet build "YoutubeRag.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "YoutubeRag.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Health check for Hangfire
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "YoutubeRag.Api.dll"]
```

### 10. Monitoring and Alerts

```csharp
// YoutubeRag.Infrastructure/Monitoring/HangfireMonitor.cs
public class HangfireMonitor : BackgroundService
{
    private readonly ILogger<HangfireMonitor> _logger;
    private readonly IConfiguration _configuration;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var stats = GetHangfireStatistics();

                // Check queue depths
                if (stats.EnqueuedCount > 100)
                {
                    _logger.LogWarning(
                        "High queue depth detected: {Count} jobs enqueued",
                        stats.EnqueuedCount);

                    // Send alert
                    await SendAlertAsync("High queue depth", stats);
                }

                // Check failed jobs
                if (stats.FailedCount > 10)
                {
                    _logger.LogError(
                        "High failure rate detected: {Count} failed jobs",
                        stats.FailedCount);

                    // Send alert
                    await SendAlertAsync("High failure rate", stats);
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring Hangfire");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private HangfireStatistics GetHangfireStatistics()
    {
        using var connection = JobStorage.Current.GetConnection();
        var stats = connection.GetStatistics();

        return new HangfireStatistics
        {
            EnqueuedCount = stats.Enqueued,
            ProcessingCount = stats.Processing,
            SucceededCount = stats.Succeeded,
            FailedCount = stats.Failed,
            ScheduledCount = stats.Scheduled,
            ServersCount = stats.Servers,
            RecurringCount = stats.Recurring
        };
    }
}
```

## Best Practices

1. **Job Idempotency**: Ensure jobs can be safely retried
2. **Timeout Configuration**: Set appropriate timeouts for each stage
3. **Resource Management**: Implement semaphores for resource-intensive operations
4. **Error Handling**: Use structured logging and detailed error messages
5. **Dashboard Security**: Always secure the Hangfire dashboard in production
6. **Database Maintenance**: Regularly clean up old job data
7. **Queue Priority**: Use different queues for different priority levels
8. **Monitoring**: Set up alerts for queue depth and failure rates
9. **Graceful Shutdown**: Handle server shutdown gracefully
10. **Testing**: Use Hangfire's testing mode for unit tests

## Troubleshooting

Common issues and solutions:

1. **Jobs stuck in processing**: Check for deadlocks, increase timeout
2. **High memory usage**: Reduce worker count, implement disposal
3. **Database connection issues**: Increase connection pool size
4. **Jobs not starting**: Verify queue names, check server status
5. **Dashboard not accessible**: Check authorization configuration