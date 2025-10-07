# SignalR Implementation Guide for Real-Time Progress Tracking

## Installation and Configuration

### 1. Required NuGet Packages

Add to `YoutubeRag.Api.csproj`:

```xml
<PackageReference Include="Microsoft.AspNetCore.SignalR" Version="1.1.0" />
<PackageReference Include="Microsoft.AspNetCore.SignalR.StackExchangeRedis" Version="8.0.0" />
```

### 2. SignalR Configuration in Program.cs

```csharp
// Add SignalR services
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(10);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.MaximumReceiveMessageSize = 102400; // 100 KB
    options.StreamBufferCapacity = 10;
})
.AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
})
.AddStackExchangeRedis(redisConnectionString, options =>
{
    options.Configuration.ChannelPrefix = "YoutubeRag";
});

// Add CORS for SignalR
builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalRPolicy", builder =>
    {
        builder.WithOrigins(
                "http://localhost:3000",
                "https://yourdomain.com")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ... other configurations ...

var app = builder.Build();

// Use CORS
app.UseCors("SignalRPolicy");

// Map SignalR hubs
app.MapHub<JobProgressHub>("/hubs/progress");
app.MapHub<NotificationHub>("/hubs/notifications");
```

## Hub Implementations

### 3. Job Progress Hub

```csharp
// YoutubeRag.Api/Hubs/JobProgressHub.cs
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

[Authorize]
public class JobProgressHub : Hub<IJobProgressClient>
{
    private readonly IJobService _jobService;
    private readonly ILogger<JobProgressHub> _logger;
    private readonly IConnectionManager _connectionManager;

    public JobProgressHub(
        IJobService jobService,
        ILogger<JobProgressHub> logger,
        IConnectionManager connectionManager)
    {
        _jobService = jobService;
        _logger = logger;
        _connectionManager = connectionManager;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("UserId")?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await _connectionManager.AddConnectionAsync(
                userId,
                Context.ConnectionId);

            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                $"user-{userId}");

            _logger.LogInformation(
                "User {UserId} connected with ConnectionId {ConnectionId}",
                userId, Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst("UserId")?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await _connectionManager.RemoveConnectionAsync(
                userId,
                Context.ConnectionId);

            _logger.LogInformation(
                "User {UserId} disconnected. ConnectionId {ConnectionId}",
                userId, Context.ConnectionId);
        }

        if (exception != null)
        {
            _logger.LogError(exception,
                "Connection {ConnectionId} disconnected with error",
                Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to job progress updates
    /// </summary>
    [HubMethodName("subscribeToJob")]
    public async Task SubscribeToJobAsync(string jobId)
    {
        var userId = Context.User?.FindFirst("UserId")?.Value;

        // Verify user owns this job
        var job = await _jobService.GetJobAsync(jobId);
        if (job == null || job.UserId != userId)
        {
            await Clients.Caller.OnError(new ErrorMessage
            {
                Code = "UNAUTHORIZED",
                Message = "You don't have access to this job"
            });
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"job-{jobId}");

        // Send current status immediately
        var status = await _jobService.GetJobStatusAsync(jobId);
        await Clients.Caller.OnJobStatusUpdate(status);

        _logger.LogInformation(
            "User {UserId} subscribed to job {JobId}",
            userId, jobId);
    }

    /// <summary>
    /// Unsubscribe from job progress updates
    /// </summary>
    [HubMethodName("unsubscribeFromJob")]
    public async Task UnsubscribeFromJobAsync(string jobId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"job-{jobId}");

        _logger.LogInformation(
            "Connection {ConnectionId} unsubscribed from job {JobId}",
            Context.ConnectionId, jobId);
    }

    /// <summary>
    /// Get all active jobs for the current user
    /// </summary>
    [HubMethodName("getActiveJobs")]
    public async Task<List<JobStatusDto>> GetActiveJobsAsync()
    {
        var userId = Context.User?.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userId))
            return new List<JobStatusDto>();

        var jobs = await _jobService.GetUserActiveJobsAsync(userId);
        return jobs;
    }

    /// <summary>
    /// Request to cancel a job
    /// </summary>
    [HubMethodName("cancelJob")]
    public async Task CancelJobAsync(string jobId)
    {
        var userId = Context.User?.FindFirst("UserId")?.Value;

        // Verify user owns this job
        var job = await _jobService.GetJobAsync(jobId);
        if (job == null || job.UserId != userId)
        {
            await Clients.Caller.OnError(new ErrorMessage
            {
                Code = "UNAUTHORIZED",
                Message = "You don't have access to this job"
            });
            return;
        }

        var cancelled = await _jobService.CancelJobAsync(jobId);

        if (cancelled)
        {
            await Clients.Group($"job-{jobId}").OnJobCancelled(new JobCancelledMessage
            {
                JobId = jobId,
                CancelledBy = userId,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}

/// <summary>
/// Client interface for strongly-typed hub
/// </summary>
public interface IJobProgressClient
{
    Task OnJobStatusUpdate(JobStatusDto status);
    Task OnJobProgress(JobProgressDto progress);
    Task OnJobCompleted(JobCompletedDto completed);
    Task OnJobFailed(JobFailedDto failed);
    Task OnJobCancelled(JobCancelledMessage cancelled);
    Task OnError(ErrorMessage error);
}
```

### 4. Progress Notification Service Implementation

```csharp
// YoutubeRag.Infrastructure/Services/SignalRProgressNotificationService.cs
using Microsoft.AspNetCore.SignalR;

public class SignalRProgressNotificationService : IProgressNotificationService
{
    private readonly IHubContext<JobProgressHub, IJobProgressClient> _hubContext;
    private readonly IConnectionManager _connectionManager;
    private readonly ILogger<SignalRProgressNotificationService> _logger;
    private readonly IMemoryCache _progressCache;

    public SignalRProgressNotificationService(
        IHubContext<JobProgressHub, IJobProgressClient> hubContext,
        IConnectionManager connectionManager,
        ILogger<SignalRProgressNotificationService> logger,
        IMemoryCache progressCache)
    {
        _hubContext = hubContext;
        _connectionManager = connectionManager;
        _logger = logger;
        _progressCache = progressCache;
    }

    public async Task NotifyProgressAsync(ProgressNotification notification)
    {
        // Throttle progress updates (max 1 per second per job)
        var cacheKey = $"progress_{notification.JobId}";
        if (_progressCache.TryGetValue<DateTime>(cacheKey, out var lastUpdate))
        {
            if (DateTime.UtcNow - lastUpdate < TimeSpan.FromSeconds(1))
                return;
        }
        _progressCache.Set(cacheKey, DateTime.UtcNow, TimeSpan.FromSeconds(1));

        var progressDto = new JobProgressDto
        {
            JobId = notification.JobId,
            Progress = notification.OverallProgress,
            CurrentStage = notification.CurrentStage,
            Message = notification.Message,
            EstimatedCompletion = notification.EstimatedCompletion,
            Timestamp = notification.Timestamp
        };

        // Send to job group
        await _hubContext.Clients
            .Group($"job-{notification.JobId}")
            .OnJobProgress(progressDto);

        // Also send to user group for dashboard updates
        await _hubContext.Clients
            .Group($"user-{notification.UserId}")
            .OnJobProgress(progressDto);

        _logger.LogDebug(
            "Sent progress notification for job {JobId}: {Progress}%",
            notification.JobId, notification.OverallProgress);
    }

    public async Task NotifyCompletionAsync(CompletionNotification notification)
    {
        var completedDto = new JobCompletedDto
        {
            JobId = notification.JobId,
            VideoId = notification.VideoId,
            VideoTitle = notification.VideoTitle,
            CompletedAt = notification.CompletedAt,
            ProcessingTime = notification.ProcessingTime,
            Stats = MapCompletionStats(notification.Stats),
            Message = notification.Message
        };

        // Send to job group
        await _hubContext.Clients
            .Group($"job-{notification.JobId}")
            .OnJobCompleted(completedDto);

        // Send to user group
        await _hubContext.Clients
            .Group($"user-{notification.UserId}")
            .OnJobCompleted(completedDto);

        // Clean up cache
        _progressCache.Remove($"progress_{notification.JobId}");

        _logger.LogInformation(
            "Sent completion notification for job {JobId}",
            notification.JobId);
    }

    public async Task NotifyFailureAsync(FailureNotification notification)
    {
        var failedDto = new JobFailedDto
        {
            JobId = notification.JobId,
            FailedStage = notification.FailedStage,
            ErrorMessage = notification.ErrorMessage,
            ErrorCode = notification.ErrorCode,
            FailedAt = notification.FailedAt,
            CanRetry = notification.CanRetry,
            RetryCount = notification.RetryCount
        };

        // Send to job group
        await _hubContext.Clients
            .Group($"job-{notification.JobId}")
            .OnJobFailed(failedDto);

        // Send to user group
        await _hubContext.Clients
            .Group($"user-{notification.UserId}")
            .OnJobFailed(failedDto);

        // Clean up cache
        _progressCache.Remove($"progress_{notification.JobId}");

        _logger.LogError(
            "Sent failure notification for job {JobId}: {Error}",
            notification.JobId, notification.ErrorMessage);
    }

    public async Task SubscribeToJobAsync(
        string userId,
        string jobId,
        string connectionId)
    {
        await _connectionManager.AddJobSubscriptionAsync(
            userId,
            jobId,
            connectionId);
    }

    public async Task UnsubscribeFromJobAsync(
        string userId,
        string jobId,
        string connectionId)
    {
        await _connectionManager.RemoveJobSubscriptionAsync(
            userId,
            jobId,
            connectionId);
    }

    public async Task BroadcastToJobSubscribersAsync(
        string jobId,
        object message)
    {
        await _hubContext.Clients
            .Group($"job-{jobId}")
            .SendAsync("onMessage", message);
    }

    public async Task<List<string>> GetUserSubscriptionsAsync(string userId)
    {
        return await _connectionManager.GetUserSubscriptionsAsync(userId);
    }

    private JobCompletionStatsDto MapCompletionStats(JobCompletionStats stats)
    {
        return new JobCompletionStatsDto
        {
            TotalSegments = stats.TotalSegments,
            TotalEmbeddings = stats.TotalEmbeddings,
            VideoDuration = stats.VideoDuration,
            TranscriptionTime = stats.TranscriptionTime,
            EmbeddingTime = stats.EmbeddingTime,
            TotalBytesProcessed = stats.TotalBytesProcessed,
            StageTimings = stats.StageTimings
        };
    }
}
```

### 5. Connection Manager

```csharp
// YoutubeRag.Infrastructure/Services/ConnectionManager.cs
public interface IConnectionManager
{
    Task AddConnectionAsync(string userId, string connectionId);
    Task RemoveConnectionAsync(string userId, string connectionId);
    Task<List<string>> GetUserConnectionsAsync(string userId);
    Task AddJobSubscriptionAsync(string userId, string jobId, string connectionId);
    Task RemoveJobSubscriptionAsync(string userId, string jobId, string connectionId);
    Task<List<string>> GetUserSubscriptionsAsync(string userId);
}

public class RedisConnectionManager : IConnectionManager
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisConnectionManager> _logger;

    public RedisConnectionManager(
        IConnectionMultiplexer redis,
        ILogger<RedisConnectionManager> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task AddConnectionAsync(string userId, string connectionId)
    {
        var db = _redis.GetDatabase();

        // Add to user's connection set
        await db.SetAddAsync($"user:connections:{userId}", connectionId);

        // Store connection metadata
        await db.StringSetAsync(
            $"connection:{connectionId}",
            JsonSerializer.Serialize(new ConnectionInfo
            {
                UserId = userId,
                ConnectedAt = DateTime.UtcNow
            }),
            TimeSpan.FromHours(1));

        _logger.LogDebug(
            "Added connection {ConnectionId} for user {UserId}",
            connectionId, userId);
    }

    public async Task RemoveConnectionAsync(string userId, string connectionId)
    {
        var db = _redis.GetDatabase();

        // Remove from user's connection set
        await db.SetRemoveAsync($"user:connections:{userId}", connectionId);

        // Remove connection metadata
        await db.KeyDeleteAsync($"connection:{connectionId}");

        // Remove all job subscriptions for this connection
        var subscriptions = await db.SetMembersAsync($"connection:jobs:{connectionId}");
        foreach (var jobId in subscriptions)
        {
            await db.SetRemoveAsync($"job:connections:{jobId}", connectionId);
        }
        await db.KeyDeleteAsync($"connection:jobs:{connectionId}");

        _logger.LogDebug(
            "Removed connection {ConnectionId} for user {UserId}",
            connectionId, userId);
    }

    public async Task<List<string>> GetUserConnectionsAsync(string userId)
    {
        var db = _redis.GetDatabase();
        var connections = await db.SetMembersAsync($"user:connections:{userId}");
        return connections.Select(c => c.ToString()).ToList();
    }

    public async Task AddJobSubscriptionAsync(
        string userId,
        string jobId,
        string connectionId)
    {
        var db = _redis.GetDatabase();

        // Add job to connection's subscription set
        await db.SetAddAsync($"connection:jobs:{connectionId}", jobId);

        // Add connection to job's subscriber set
        await db.SetAddAsync($"job:connections:{jobId}", connectionId);

        _logger.LogDebug(
            "Added job subscription: User {UserId}, Job {JobId}, Connection {ConnectionId}",
            userId, jobId, connectionId);
    }

    public async Task RemoveJobSubscriptionAsync(
        string userId,
        string jobId,
        string connectionId)
    {
        var db = _redis.GetDatabase();

        // Remove job from connection's subscription set
        await db.SetRemoveAsync($"connection:jobs:{connectionId}", jobId);

        // Remove connection from job's subscriber set
        await db.SetRemoveAsync($"job:connections:{jobId}", connectionId);

        _logger.LogDebug(
            "Removed job subscription: User {UserId}, Job {JobId}, Connection {ConnectionId}",
            userId, jobId, connectionId);
    }

    public async Task<List<string>> GetUserSubscriptionsAsync(string userId)
    {
        var db = _redis.GetDatabase();
        var connections = await GetUserConnectionsAsync(userId);
        var allSubscriptions = new HashSet<string>();

        foreach (var connectionId in connections)
        {
            var jobs = await db.SetMembersAsync($"connection:jobs:{connectionId}");
            foreach (var job in jobs)
            {
                allSubscriptions.Add(job.ToString());
            }
        }

        return allSubscriptions.ToList();
    }
}

public class ConnectionInfo
{
    public string UserId { get; set; } = string.Empty;
    public DateTime ConnectedAt { get; set; }
}
```

## Client Implementation Examples

### 6. JavaScript/TypeScript Client

```typescript
// signalr-client.ts
import * as signalR from "@microsoft/signalr";

export class JobProgressClient {
  private connection: signalR.HubConnection;
  private connectionPromise: Promise<void>;

  constructor(private baseUrl: string, private accessToken: string) {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${baseUrl}/hubs/progress`, {
        accessTokenFactory: () => this.accessToken,
        transport: signalR.HttpTransportType.WebSockets |
                   signalR.HttpTransportType.ServerSentEvents |
                   signalR.HttpTransportType.LongPolling
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: retryContext => {
          if (retryContext.elapsedMilliseconds < 60000) {
            // Retry every 2 seconds for first minute
            return 2000;
          } else {
            // Then retry every 10 seconds
            return 10000;
          }
        }
      })
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.setupEventHandlers();
    this.connectionPromise = this.start();
  }

  private setupEventHandlers(): void {
    // Progress updates
    this.connection.on("onJobProgress", (progress: JobProgressDto) => {
      console.log(`Job ${progress.jobId}: ${progress.progress}% - ${progress.message}`);
      this.onProgressUpdate(progress);
    });

    // Job completed
    this.connection.on("onJobCompleted", (completed: JobCompletedDto) => {
      console.log(`Job ${completed.jobId} completed!`);
      this.onJobCompleted(completed);
    });

    // Job failed
    this.connection.on("onJobFailed", (failed: JobFailedDto) => {
      console.error(`Job ${failed.jobId} failed: ${failed.errorMessage}`);
      this.onJobFailed(failed);
    });

    // Job cancelled
    this.connection.on("onJobCancelled", (cancelled: JobCancelledMessage) => {
      console.log(`Job ${cancelled.jobId} cancelled`);
      this.onJobCancelled(cancelled);
    });

    // Error messages
    this.connection.on("onError", (error: ErrorMessage) => {
      console.error(`Error: ${error.message}`);
      this.onError(error);
    });

    // Connection state changes
    this.connection.onreconnecting(() => {
      console.log("Reconnecting to SignalR...");
      this.onReconnecting();
    });

    this.connection.onreconnected(() => {
      console.log("Reconnected to SignalR");
      this.onReconnected();
    });

    this.connection.onclose(() => {
      console.log("SignalR connection closed");
      this.onConnectionClosed();
    });
  }

  private async start(): Promise<void> {
    try {
      await this.connection.start();
      console.log("SignalR connected");
    } catch (err) {
      console.error("Failed to connect to SignalR:", err);
      setTimeout(() => this.start(), 5000);
    }
  }

  public async subscribeToJob(jobId: string): Promise<void> {
    await this.connectionPromise;
    await this.connection.invoke("subscribeToJob", jobId);
  }

  public async unsubscribeFromJob(jobId: string): Promise<void> {
    await this.connectionPromise;
    await this.connection.invoke("unsubscribeFromJob", jobId);
  }

  public async cancelJob(jobId: string): Promise<void> {
    await this.connectionPromise;
    await this.connection.invoke("cancelJob", jobId);
  }

  public async getActiveJobs(): Promise<JobStatusDto[]> {
    await this.connectionPromise;
    return await this.connection.invoke<JobStatusDto[]>("getActiveJobs");
  }

  // Event handlers to be overridden
  protected onProgressUpdate(progress: JobProgressDto): void {}
  protected onJobCompleted(completed: JobCompletedDto): void {}
  protected onJobFailed(failed: JobFailedDto): void {}
  protected onJobCancelled(cancelled: JobCancelledMessage): void {}
  protected onError(error: ErrorMessage): void {}
  protected onReconnecting(): void {}
  protected onReconnected(): void {}
  protected onConnectionClosed(): void {}

  public async disconnect(): Promise<void> {
    await this.connection.stop();
  }
}

// DTOs
interface JobProgressDto {
  jobId: string;
  progress: number;
  currentStage: string;
  message: string;
  estimatedCompletion?: Date;
  timestamp: Date;
}

interface JobCompletedDto {
  jobId: string;
  videoId: string;
  videoTitle: string;
  completedAt: Date;
  processingTime: string;
  stats: JobCompletionStatsDto;
  message: string;
}

interface JobFailedDto {
  jobId: string;
  failedStage: string;
  errorMessage: string;
  errorCode: string;
  failedAt: Date;
  canRetry: boolean;
  retryCount: number;
}

interface JobCancelledMessage {
  jobId: string;
  cancelledBy: string;
  timestamp: Date;
}

interface ErrorMessage {
  code: string;
  message: string;
}

interface JobCompletionStatsDto {
  totalSegments: number;
  totalEmbeddings: number;
  videoDuration: string;
  transcriptionTime: string;
  embeddingTime: string;
  totalBytesProcessed: number;
  stageTimings: Record<string, string>;
}
```

### 7. React Component Example

```tsx
// JobProgressTracker.tsx
import React, { useEffect, useState } from 'react';
import { JobProgressClient } from './signalr-client';

interface JobProgressTrackerProps {
  jobId: string;
  accessToken: string;
}

export const JobProgressTracker: React.FC<JobProgressTrackerProps> = ({
  jobId,
  accessToken
}) => {
  const [progress, setProgress] = useState(0);
  const [currentStage, setCurrentStage] = useState('');
  const [status, setStatus] = useState<'pending' | 'processing' | 'completed' | 'failed'>('pending');
  const [error, setError] = useState<string | null>(null);
  const [client, setClient] = useState<JobProgressClient | null>(null);

  useEffect(() => {
    const progressClient = new JobProgressClient(
      process.env.REACT_APP_API_URL || 'http://localhost:5000',
      accessToken
    ) as any;

    // Override event handlers
    progressClient.onProgressUpdate = (progress: any) => {
      setProgress(progress.progress);
      setCurrentStage(progress.currentStage);
      setStatus('processing');
    };

    progressClient.onJobCompleted = (completed: any) => {
      setProgress(100);
      setStatus('completed');
    };

    progressClient.onJobFailed = (failed: any) => {
      setError(failed.errorMessage);
      setStatus('failed');
    };

    progressClient.onError = (error: any) => {
      setError(error.message);
    };

    // Subscribe to job
    progressClient.subscribeToJob(jobId);

    setClient(progressClient);

    // Cleanup
    return () => {
      progressClient.unsubscribeFromJob(jobId);
      progressClient.disconnect();
    };
  }, [jobId, accessToken]);

  const handleCancel = async () => {
    if (client) {
      await client.cancelJob(jobId);
    }
  };

  return (
    <div className="job-progress-tracker">
      <div className="progress-header">
        <h3>Processing Video</h3>
        <span className={`status-badge status-${status}`}>
          {status}
        </span>
      </div>

      {status === 'processing' && (
        <div className="progress-details">
          <div className="stage-info">
            <span>Current Stage:</span>
            <strong>{currentStage}</strong>
          </div>

          <div className="progress-bar-container">
            <div
              className="progress-bar"
              style={{ width: `${progress}%` }}
            >
              <span className="progress-text">{progress}%</span>
            </div>
          </div>

          <button
            onClick={handleCancel}
            className="cancel-button"
            disabled={status !== 'processing'}
          >
            Cancel Processing
          </button>
        </div>
      )}

      {status === 'completed' && (
        <div className="success-message">
          ✅ Video processing completed successfully!
        </div>
      )}

      {status === 'failed' && error && (
        <div className="error-message">
          ❌ Processing failed: {error}
        </div>
      )}
    </div>
  );
};
```

## Testing SignalR

### 8. Integration Tests

```csharp
// YoutubeRag.Tests/Integration/SignalRTests.cs
[TestClass]
public class SignalRIntegrationTests
{
    private WebApplicationFactory<Program> _factory;
    private HubConnection _connection;

    [TestInitialize]
    public async Task Initialize()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Add test services
                });
            });

        var client = _factory.CreateClient();
        var baseUrl = client.BaseAddress?.ToString() ?? "http://localhost";

        _connection = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}hubs/progress", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        await _connection.StartAsync();
    }

    [TestMethod]
    public async Task SubscribeToJob_ValidJob_ReceivesUpdates()
    {
        // Arrange
        var jobId = "test-job-123";
        var progressReceived = new TaskCompletionSource<JobProgressDto>();

        _connection.On<JobProgressDto>("onJobProgress", progress =>
        {
            progressReceived.SetResult(progress);
        });

        // Act
        await _connection.InvokeAsync("subscribeToJob", jobId);

        // Simulate progress update
        await SimulateProgressUpdate(jobId, 50, "Processing");

        // Assert
        var progress = await progressReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.AreEqual(jobId, progress.JobId);
        Assert.AreEqual(50, progress.Progress);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _connection.DisposeAsync();
        _factory.Dispose();
    }
}
```

## Performance Optimization

### 9. Message Throttling

```csharp
// YoutubeRag.Infrastructure/Services/ThrottledProgressNotificationService.cs
public class ThrottledProgressNotificationService : IProgressNotificationService
{
    private readonly IProgressNotificationService _innerService;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _throttleInterval = TimeSpan.FromMilliseconds(500);

    public async Task NotifyProgressAsync(ProgressNotification notification)
    {
        var key = $"progress_{notification.JobId}";
        var lastNotification = _cache.Get<ProgressNotification>(key);

        // Only send if significant change or time elapsed
        if (ShouldSendUpdate(lastNotification, notification))
        {
            await _innerService.NotifyProgressAsync(notification);
            _cache.Set(key, notification, _throttleInterval);
        }
    }

    private bool ShouldSendUpdate(
        ProgressNotification? last,
        ProgressNotification current)
    {
        if (last == null)
            return true;

        // Always send stage changes
        if (last.CurrentStage != current.CurrentStage)
            return true;

        // Send if progress changed by at least 5%
        if (Math.Abs(last.OverallProgress - current.OverallProgress) >= 5)
            return true;

        return false;
    }
}
```

## Monitoring and Metrics

### 10. SignalR Metrics

```csharp
// YoutubeRag.Infrastructure/Monitoring/SignalRMetrics.cs
public class SignalRMetrics : IHostedService
{
    private readonly IHubContext<JobProgressHub> _hubContext;
    private readonly IConnectionManager _connectionManager;
    private readonly ILogger<SignalRMetrics> _logger;
    private Timer _timer;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _timer = new Timer(
            async _ => await CollectMetrics(),
            null,
            TimeSpan.Zero,
            TimeSpan.FromMinutes(1));
    }

    private async Task CollectMetrics()
    {
        var metrics = new SignalRMetricsData
        {
            Timestamp = DateTime.UtcNow,
            ActiveConnections = await GetActiveConnectionCount(),
            ActiveSubscriptions = await GetActiveSubscriptionCount(),
            MessagesPerMinute = GetMessageRate()
        };

        _logger.LogInformation(
            "SignalR Metrics: Connections={Connections}, Subscriptions={Subscriptions}, Messages/min={Messages}",
            metrics.ActiveConnections,
            metrics.ActiveSubscriptions,
            metrics.MessagesPerMinute);

        // Send to monitoring system
        await SendToMonitoring(metrics);
    }
}
```

## Best Practices

1. **Connection Management**: Always handle reconnection scenarios
2. **Message Size**: Keep messages small (< 32KB recommended)
3. **Throttling**: Implement client and server-side throttling
4. **Groups**: Use groups for efficient message routing
5. **Authentication**: Always authenticate hub connections
6. **Error Handling**: Implement comprehensive error handling
7. **Scaling**: Use Redis backplane for multi-server deployments
8. **Monitoring**: Track connection counts and message rates
9. **Testing**: Test connection failures and reconnection scenarios
10. **Security**: Validate all incoming data and sanitize outputs