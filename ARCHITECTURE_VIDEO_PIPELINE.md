# YouTube RAG Video Ingestion Pipeline Architecture

## Executive Summary

This document outlines the architecture for a scalable, resilient video processing pipeline for the YouTube RAG application. The system processes YouTube videos through multiple stages: metadata extraction, transcription, segmentation, embedding generation, and persistence. The architecture supports both local and cloud-based processing modes, handles long-running operations asynchronously, provides real-time progress tracking, and ensures fault tolerance through retry mechanisms and circuit breakers.

## Table of Contents

1. [Current State Assessment](#current-state-assessment)
2. [Proposed Architecture](#proposed-architecture)
3. [Pipeline Stage Design](#pipeline-stage-design)
4. [Technology Stack & Recommendations](#technology-stack--recommendations)
5. [Data Model Design](#data-model-design)
6. [Service Architecture](#service-architecture)
7. [Implementation Plan](#implementation-plan)
8. [Scalability Considerations](#scalability-considerations)
9. [Testing Strategy](#testing-strategy)

---

## Current State Assessment

### Existing Components

The current implementation includes:

- **Domain Entities**: `Video`, `Job`, `TranscriptSegment` with basic relationships
- **Services**: `IVideoProcessingService`, `IJobService` with preliminary interfaces
- **Infrastructure**: YouTube, Transcription, and Embedding service interfaces
- **Clean Architecture**: Well-structured layers (Domain, Application, Infrastructure, API)

### Gaps to Address

1. No background job processing infrastructure
2. Limited job state tracking (basic enum states)
3. No real-time progress reporting mechanism
4. Missing retry and resilience patterns
5. No pipeline orchestration layer
6. Insufficient error handling and recovery

---

## Proposed Architecture

### High-Level Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                           Client Layer                              │
│  ┌─────────────┐  ┌──────────────┐  ┌─────────────────────────┐   │
│  │   Web UI    │  │  Mobile App  │  │      API Clients        │   │
│  └──────┬──────┘  └──────┬───────┘  └──────────┬──────────────┘   │
└─────────┼─────────────────┼─────────────────────┼──────────────────┘
          │                 │                     │
          └─────────────────┴─────────────────────┘
                            │
                    SignalR / WebSocket
                            │
┌───────────────────────────┴─────────────────────────────────────────┐
│                         API Gateway Layer                           │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │              ASP.NET Core Web API (YoutubeRag.Api)           │  │
│  │  ┌──────────┐ ┌───────────┐ ┌──────────┐ ┌──────────────┐  │  │
│  │  │  Videos  │ │   Jobs    │ │  Search  │ │   Progress   │  │  │
│  │  │Controller│ │Controller │ │Controller│ │  Hub (SignalR)│  │  │
│  │  └──────────┘ └───────────┘ └──────────┘ └──────────────┘  │  │
│  └──────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────┘
                            │
┌───────────────────────────┴─────────────────────────────────────────┐
│                    Application Layer                                 │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │                    Orchestration Services                      │ │
│  │  ┌──────────────────┐  ┌──────────────────────────────────┐  │ │
│  │  │ VideoIngestion   │  │      JobOrchestration            │  │ │
│  │  │    Service       │  │         Service                  │  │ │
│  │  └──────────────────┘  └──────────────────────────────────┘  │ │
│  └────────────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │                    Pipeline Stage Handlers                     │ │
│  │  ┌─────────┐ ┌──────────┐ ┌───────────┐ ┌──────────────┐    │ │
│  │  │Metadata │ │Transcript│ │Segmentation│ │  Embedding   │    │ │
│  │  │Handler  │ │ Handler  │ │  Handler   │ │   Handler    │    │ │
│  │  └─────────┘ └──────────┘ └────────────┘ └──────────────┘    │ │
│  └────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────┘
                            │
┌───────────────────────────┴─────────────────────────────────────────┐
│                    Infrastructure Layer                              │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │                 Background Job Processing                       │ │
│  │  ┌──────────────┐  ┌─────────────┐  ┌───────────────────┐    │ │
│  │  │   Hangfire   │  │ Job Queue   │  │  Progress Cache   │    │ │
│  │  │   Server     │  │  (Redis)    │  │     (Redis)       │    │ │
│  │  └──────────────┘  └─────────────┘  └───────────────────┘    │ │
│  └────────────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │                    External Service Integrations                │ │
│  │  ┌──────────────────┐  ┌──────────────┐  ┌──────────────────┐ │ │
│  │  │  YouTube Audio   │  │   Whisper    │  │ Embedding Service│ │ │
│  │  │  YoutubeExplode  │  │  (Local/API) │  │(SentenceTransform│ │ │
│  │  │  + yt-dlp Fallbk │  │              │  │  ers/OpenAI)     │ │ │
│  │  └──────────────────┘  └──────────────┘  └──────────────────┘ │ │
│  └────────────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │                        Data Persistence                         │ │
│  │  ┌──────────────┐  ┌─────────────────┐  ┌─────────────────┐  │ │
│  │  │    MySQL     │  │   Redis Cache   │  │  File Storage   │  │ │
│  │  │  (EF Core)   │  │                 │  │  (Local/Cloud)  │  │ │
│  │  └──────────────┘  └─────────────────┘  └─────────────────┘  │ │
│  └────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────┘
```

### Data Flow Diagram

```
User Submits URL
       │
       ▼
[API Controller] ──────► [Create Job Record]
       │                         │
       │                         ▼
       ▼                  [MySQL Database]
[Enqueue Background Job]         │
       │                         │
       ▼                         ▼
[Hangfire Queue] ────► [Job Orchestrator]
       │                         │
       ▼                         ▼
[Pipeline Executor] ◄────────────┘
       │
       ├──► Stage 1: [Metadata Extraction] ──► YouTube API
       │                 │
       │                 ▼
       │            [Update Job Progress] ──► Redis Cache
       │                                          │
       │                                          ▼
       ├──► Stage 2: [Transcription] ──────► Whisper Service
       │                 │
       │                 ▼
       │            [Update Job Progress] ──► SignalR Hub
       │                                          │
       ├──► Stage 3: [Segmentation]              ▼
       │                 │                   [Client Updates]
       │                 ▼
       ├──► Stage 4: [Embedding Generation] ──► Embedding Service
       │                 │
       │                 ▼
       └──► Stage 5: [Persistence] ────────► MySQL Database
                         │
                         ▼
                  [Complete Job] ───────────► Notify Client
```

---

## Pipeline Stage Design

### Stage 1: Metadata Extraction

```csharp
public class MetadataExtractionStage : IPipelineStage
{
    // Input
    public class Input
    {
        public string YouTubeUrl { get; set; }
        public string JobId { get; set; }
    }

    // Output
    public class Output
    {
        public string VideoId { get; set; }
        public VideoMetadata Metadata { get; set; }
    }

    // Processing
    - Validate URL format
    - Extract video ID from URL
    - Call YouTube Data API v3
    - Parse response (title, description, duration, channel)
    - Store metadata in Video entity

    // Error Handling
    - Invalid URL → ValidationException
    - Private/deleted video → VideoNotFoundException
    - API rate limit → RateLimitException (retry with backoff)
    - Network failure → NetworkException (retry)

    // Estimated Time: 2-5 seconds
    // Progress Weight: 5%
}
```

### Stage 1.5: Audio Extraction

```csharp
public class AudioExtractionStage : IPipelineStage
{
    // Input
    public class Input
    {
        public string YouTubeId { get; set; }
        public string JobId { get; set; }
    }

    // Output
    public class Output
    {
        public string AudioFilePath { get; set; }
        public string Format { get; set; }
        public TimeSpan Duration { get; set; }
    }

    // Processing Steps - Fallback Pattern Implementation
    1. **Primary Method: YoutubeExplode**
       - Attempt to extract audio using YoutubeExplode library
       - Get stream manifest for the video
       - Select best audio-only stream
       - Download and save to local storage

    2. **Fallback Method: yt-dlp (on 403 Forbidden)**
       - If YoutubeExplode fails with 403 error (YouTube blocking)
       - Automatically fall back to yt-dlp command-line tool
       - Extract audio with configurable quality settings
       - Supports multiple output formats (mp3, webm, etc.)

    // Implementation Details
    try
    {
        // Try YoutubeExplode first
        var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(videoId);
        var audioStream = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
        await _youtubeClient.Videos.Streams.DownloadAsync(audioStream, filePath);
        return filePath;
    }
    catch (HttpRequestException ex) when (ex.Message.Contains("403"))
    {
        // Fall back to yt-dlp when YouTube blocks YoutubeExplode
        _logger.LogWarning("YoutubeExplode blocked with 403. Falling back to yt-dlp");
        return await ExtractAudioUsingYtDlpAsync(videoId, cancellationToken);
    }

    // yt-dlp Executable Discovery
    - Searches multiple common installation paths
    - Windows: Python Scripts directory, user AppData
    - Linux/Mac: /usr/local/bin, /usr/bin, ~/.local/bin
    - Validates executable with --help command before use

    // yt-dlp Command Structure
    Arguments: "-x --audio-format mp3 --audio-quality 0 -o {outputPath} {videoUrl}"
    - -x: Extract audio
    - --audio-format mp3: Convert to MP3
    - --audio-quality 0: Best quality
    - Timeout: 30 minutes for extraction process

    // Error Handling
    - 403 Forbidden (YoutubeExplode) → Automatic fallback to yt-dlp
    - yt-dlp not installed → InvalidOperationException with install instructions
    - yt-dlp timeout (>30 min) → TimeoutException
    - Invalid video ID → VideoNotFoundException
    - Network failure → NetworkException (retry with backoff)
    - No audio stream available → NoAudioException

    // Resilience Features
    - Automatic fallback on YouTube blocking (403 errors)
    - Timeout handling prevents hung processes
    - Unicode encoding support (PYTHONIOENCODING=utf-8)
    - Process cleanup on cancellation or timeout

    // Estimated Time: 10-120 seconds (based on video duration and method)
    // Progress Weight: 15%

    // Notes
    - YouTube frequently changes their API/format, causing 403 blocks
    - yt-dlp is actively maintained and bypasses most YouTube restrictions
    - Fallback pattern ensures resilience against YouTube changes
    - Install yt-dlp with: pip install yt-dlp
}
```

### Stage 2: Transcription Acquisition

```csharp
public class TranscriptionStage : IPipelineStage
{
    // Input
    public class Input
    {
        public string VideoId { get; set; }
        public string AudioFilePath { get; set; } // From Audio Extraction Stage
        public string JobId { get; set; }
        public ProcessingMode Mode { get; set; } // Local or Real
    }

    // Output
    public class Output
    {
        public List<TranscriptionSegment> Segments { get; set; }
        public string Language { get; set; }
        public double Confidence { get; set; }
    }

    // Processing Steps
    1. Check for YouTube captions (if available)
    2. If no captions or low quality:
       - Local Mode: Use audio file from Stage 1.5 → Run local Whisper
       - Real Mode: Stream audio to OpenAI Whisper API
    3. Parse transcription with timestamps
    4. Detect language and confidence scores

    // Error Handling
    - Audio file not found → FileNotFoundException
    - Whisper timeout (>60 min) → TimeoutException (kill process, retry)
    - Invalid format → FormatException
    - Service unavailable → ServiceException (circuit breaker)

    // Whisper Timeout Handling (IMPLEMENTED)
    - Uses WaitForExitAsync with 60-minute timeout
    - Kills hung processes gracefully
    - Prevents indefinite hangs that block job queue
    - Logs timeout errors for monitoring

    // Estimated Time: 30 seconds - 10 minutes (based on duration)
    // Progress Weight: 40%
}
```

### Stage 3: Segmentation

```csharp
public class SegmentationStage : IPipelineStage
{
    // Input
    public class Input
    {
        public List<TranscriptionSegment> RawSegments { get; set; }
        public SegmentationStrategy Strategy { get; set; }
    }

    // Output
    public class Output
    {
        public List<ProcessedSegment> Segments { get; set; }
        public int TotalSegments { get; set; }
    }

    // Segmentation Strategies
    1. TimeBased: Fixed time windows (e.g., 30-second chunks)
    2. Semantic: Natural language boundaries (sentences/paragraphs)
    3. Hybrid: Semantic with max time constraint

    // Processing
    - Apply chosen segmentation strategy
    - Maintain context overlap (sliding window)
    - Add segment metadata (index, timestamps)
    - Optimize for embedding model input size

    // Error Handling
    - Empty transcription → EmptyContentException
    - Segmentation failure → SegmentationException

    // Estimated Time: 5-15 seconds
    // Progress Weight: 10%
}
```

### Stage 4: Embedding Generation

```csharp
public class EmbeddingGenerationStage : IPipelineStage
{
    // Input
    public class Input
    {
        public List<ProcessedSegment> Segments { get; set; }
        public EmbeddingModel Model { get; set; }
    }

    // Output
    public class Output
    {
        public List<SegmentWithEmbedding> Segments { get; set; }
        public int VectorDimension { get; set; }
    }

    // Processing
    - Batch segments for efficient processing
    - Local Mode: Use sentence-transformers (384 dimensions)
    - Real Mode: Use OpenAI embeddings (1536 dimensions)
    - Normalize vectors for cosine similarity
    - Store as JSON array or binary format

    // Error Handling
    - Model loading failure → ModelException
    - API quota exceeded → QuotaException (retry with backoff)
    - Invalid text → ValidationException (skip segment)

    // Estimated Time: 10-60 seconds (based on segment count)
    // Progress Weight: 35%
}
```

### Stage 5: Persistence

```csharp
public class PersistenceStage : IPipelineStage
{
    // Input
    public class Input
    {
        public Video Video { get; set; }
        public List<TranscriptSegment> Segments { get; set; }
    }

    // Output
    public class Output
    {
        public string VideoId { get; set; }
        public int SegmentCount { get; set; }
        public bool Success { get; set; }
    }

    // Processing
    - Begin database transaction
    - Update Video status to Completed
    - Bulk insert TranscriptSegments
    - Update search indices
    - Commit transaction

    // Error Handling
    - Database connection failure → DatabaseException (retry)
    - Transaction rollback on failure
    - Partial save recovery

    // Estimated Time: 2-5 seconds
    // Progress Weight: 10%
}
```

---

## Technology Stack & Recommendations

### Background Job Processing: Hangfire (Recommended)

**Why Hangfire:**
- Native .NET integration with excellent documentation
- Built-in dashboard for monitoring
- Supports multiple storage backends (MySQL compatible)
- Automatic retries with configurable policies
- Recurring jobs and continuations
- Fire-and-forget, delayed, and batch job support

**Configuration:**

```csharp
// Program.cs
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseStorage(new MySqlStorage(connectionString, new MySqlStorageOptions
    {
        QueuePollInterval = TimeSpan.FromSeconds(2),
        JobExpirationCheckInterval = TimeSpan.FromHours(1),
        CountersAggregateInterval = TimeSpan.FromMinutes(5),
        PrepareSchemaIfNecessary = true,
        DashboardJobListLimit = 50000,
        TransactionTimeout = TimeSpan.FromMinutes(1),
    })));

builder.Services.AddHangfireServer(options =>
{
    options.Queues = new[] { "critical", "default", "low" };
    options.WorkerCount = Environment.ProcessorCount * 2;
});
```

### Progress Tracking: SignalR (Recommended)

**Why SignalR:**
- Real-time bidirectional communication
- Automatic reconnection handling
- Fallback to long polling if WebSockets unavailable
- Built into ASP.NET Core
- Scales with Redis backplane

**Implementation:**

```csharp
public interface IProgressHub
{
    Task UpdateProgress(JobProgressUpdate update);
    Task JobCompleted(JobCompletionNotification notification);
    Task JobFailed(JobFailureNotification notification);
}

public class JobProgressHub : Hub<IProgressHub>
{
    public async Task SubscribeToJob(string jobId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"job-{jobId}");
    }

    public async Task UnsubscribeFromJob(string jobId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"job-{jobId}");
    }
}
```

### Caching Strategy: Redis

**Usage:**
- Job progress caching (TTL: 1 hour after completion)
- Distributed lock for job processing
- SignalR backplane for scaling
- Rate limiting counters
- Circuit breaker state

### Resilience Patterns: Polly

```csharp
// Retry Policy
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
    .WaitAndRetryAsync(
        3,
        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (outcome, timespan, retry, context) =>
        {
            logger.LogWarning($"Retry {retry} after {timespan}");
        });

// Circuit Breaker
var circuitBreakerPolicy = Policy
    .Handle<HttpRequestException>()
    .CircuitBreakerAsync(
        3,
        TimeSpan.FromMinutes(1),
        onBreak: (result, timespan) => logger.LogError($"Circuit broken for {timespan}"),
        onReset: () => logger.LogInformation("Circuit reset"));

// Combine policies
var resilientPolicy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
```

---

## Data Model Design

### Enhanced Entity Models

```csharp
// New Entity: JobStage
public class JobStage : BaseEntity
{
    public string JobId { get; set; }
    public string StageName { get; set; }
    public StageStatus Status { get; set; }
    public int Progress { get; set; }
    public int Order { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? OutputData { get; set; } // JSON
    public int RetryCount { get; set; }

    // Navigation
    public virtual Job Job { get; set; }
}

// New Enum: StageStatus
public enum StageStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Skipped,
    Retrying
}

// Enhanced Job Entity
public class Job : BaseEntity
{
    // ... existing properties ...

    public string PipelineType { get; set; } // "VideoIngestion"
    public DateTime? EstimatedCompletion { get; set; }
    public int Priority { get; set; } = 0;
    public string? ParentJobId { get; set; }

    // Navigation
    public virtual ICollection<JobStage> Stages { get; set; }
    public virtual Job? ParentJob { get; set; }
    public virtual ICollection<Job> ChildJobs { get; set; }
}

// New Entity: ProcessingConfiguration
public class ProcessingConfiguration : BaseEntity
{
    public string ConfigKey { get; set; }
    public string ConfigValue { get; set; } // JSON
    public string Category { get; set; }
    public bool IsActive { get; set; }
}
```

### State Machine

```
Job State Machine:
==================
                     ┌─────────┐
                     │ Pending │
                     └────┬────┘
                          │ Start
                          ▼
                     ┌─────────┐
               ┌─────│ Running │─────┐
               │     └────┬────┘     │
               │          │          │
           Cancel         │        Error
               │          │          │
               ▼          ▼          ▼
         ┌──────────┐ ┌─────────┐ ┌────────┐
         │Cancelled │ │Completed│ │ Failed │
         └──────────┘ └─────────┘ └────┬───┘
                                        │
                                    Retry│
                                        │
                                   ┌────▼────┐
                                   │Retrying │
                                   └────┬────┘
                                        │
                                        └──────┐
                                               │
                                               ▼
                                          [Running]

Stage State Machine:
===================
Pending → Running → Completed
   ↓         ↓          ↑
Skipped   Failed ───────┘
            ↓      (retry)
         Retrying
```

---

## Service Architecture

### Application Layer Services

```csharp
// IVideoIngestionService.cs
public interface IVideoIngestionService
{
    Task<string> StartIngestionAsync(VideoIngestionRequest request);
    Task<VideoIngestionStatus> GetIngestionStatusAsync(string jobId);
    Task<bool> CancelIngestionAsync(string jobId);
    Task<List<VideoIngestionHistory>> GetIngestionHistoryAsync(string userId);
}

// IJobOrchestrationService.cs
public interface IJobOrchestrationService
{
    Task<T> ExecutePipelineAsync<T>(IPipelineDefinition pipeline, object input) where T : class;
    Task UpdateStageProgressAsync(string jobId, string stageName, int progress);
    Task HandleStageFailureAsync(string jobId, string stageName, Exception exception);
    Task<bool> ShouldRetryStageAsync(string jobId, string stageName);
}

// IProgressNotificationService.cs
public interface IProgressNotificationService
{
    Task NotifyProgressAsync(string jobId, JobProgress progress);
    Task NotifyCompletionAsync(string jobId, JobResult result);
    Task NotifyFailureAsync(string jobId, string errorMessage);
}

// IPipelineStage.cs
public interface IPipelineStage
{
    string Name { get; }
    int Order { get; }
    int Weight { get; } // For progress calculation
    Task<StageResult> ExecuteAsync(StageContext context);
    Task<bool> CanExecuteAsync(StageContext context);
    Task CompensateAsync(StageContext context); // For rollback
}

// Pipeline Definition
public class VideoIngestionPipeline : IPipelineDefinition
{
    public string Name => "VideoIngestion";
    public List<IPipelineStage> Stages => new()
    {
        new MetadataExtractionStage(),
        new TranscriptionStage(),
        new SegmentationStage(),
        new EmbeddingGenerationStage(),
        new PersistenceStage()
    };
}
```

### Infrastructure Layer Services

```csharp
// IBackgroundJobService.cs
public interface IBackgroundJobService
{
    string EnqueueJob<T>(Expression<Func<T, Task>> methodCall, JobPriority priority = JobPriority.Default);
    string ScheduleJob<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay);
    bool CancelJob(string jobId);
    JobStatus GetJobStatus(string jobId);
    void ConfigureRecurringJob(string jobId, Expression<Func<Task>> methodCall, string cronExpression);
}

// Hangfire Implementation
public class HangfireBackgroundJobService : IBackgroundJobService
{
    public string EnqueueJob<T>(Expression<Func<T, Task>> methodCall, JobPriority priority = JobPriority.Default)
    {
        var queue = priority switch
        {
            JobPriority.Critical => "critical",
            JobPriority.High => "default",
            JobPriority.Low => "low",
            _ => "default"
        };

        return BackgroundJob.Enqueue(methodCall, queue);
    }
}

// Enhanced YouTube/Audio Extraction Service
public interface IAudioExtractionService
{
    // Primary extraction method with automatic fallback
    Task<string> ExtractAudioFromYouTubeAsync(string youTubeId, CancellationToken cancellationToken = default);

    // Fallback method using yt-dlp (called automatically on 403 errors)
    Task<string> ExtractAudioUsingYtDlpAsync(string youTubeId, CancellationToken cancellationToken = default);

    // Utility methods
    string FindYtDlpExecutable();
    Task<bool> IsYtDlpAvailableAsync();
}

public interface IYouTubeService
{
    Task<VideoMetadata> GetVideoMetadataAsync(string videoId);
    Task<CaptionTrack[]> GetAvailableCaptionsAsync(string videoId);
    Task<string> DownloadCaptionsAsync(string videoId, string language);
    Task<Stream> DownloadAudioStreamAsync(string videoId);
}

// Enhanced Transcription Service
public interface ITranscriptionService
{
    Task<TranscriptionResult> TranscribeAsync(Stream audioStream, TranscriptionOptions options);
    Task<bool> IsServiceAvailableAsync();
    TranscriptionCapabilities GetCapabilities();
}

// Enhanced Embedding Service
public interface IEmbeddingService
{
    Task<float[][]> GenerateEmbeddingsAsync(string[] texts, EmbeddingOptions options);
    Task<bool> IsServiceAvailableAsync();
    EmbeddingCapabilities GetCapabilities();
}
```

---

## Implementation Plan

### Phase 1: Foundation (Week 1-2)

1. **Install and Configure Hangfire**
   - Add Hangfire NuGet packages
   - Configure MySQL storage
   - Set up dashboard authentication
   - Create job queues (critical, default, low)

2. **Implement Job State Tracking**
   - Create JobStage entity
   - Add migrations for new entities
   - Implement state machine logic
   - Create job repository methods

3. **Set Up SignalR**
   - Configure SignalR hubs
   - Implement progress notification service
   - Create client connection management
   - Add Redis backplane for scaling

**Estimated Effort:** 40 hours

### Phase 2: Pipeline Infrastructure (Week 3-4)

1. **Create Pipeline Framework**
   - Implement IPipelineStage interface
   - Build PipelineExecutor
   - Add stage retry logic
   - Implement compensation/rollback

2. **Build Job Orchestration Service**
   - Implement IJobOrchestrationService
   - Add progress tracking
   - Handle stage failures
   - Implement retry policies

3. **Add Resilience Patterns**
   - Configure Polly policies
   - Implement circuit breakers
   - Add timeout handling
   - Create fallback strategies

**Estimated Effort:** 60 hours

### Phase 3: Pipeline Stages (Week 5-6)

1. **Implement Metadata Extraction Stage**
   - YouTube API integration
   - Error handling
   - Progress reporting

2. **Implement Transcription Stage**
   - Local Whisper integration
   - OpenAI API integration
   - Caption fallback logic

3. **Implement Segmentation Stage**
   - Multiple strategy support
   - Context overlap handling

4. **Implement Embedding Stage**
   - Batch processing
   - Local and API modes
   - Vector normalization

5. **Implement Persistence Stage**
   - Transaction management
   - Bulk insert optimization

**Estimated Effort:** 80 hours

### Phase 4: Integration & Testing (Week 7-8)

1. **End-to-End Integration**
   - Wire up all components
   - Test complete pipeline
   - Performance optimization

2. **Monitoring & Observability**
   - Add structured logging
   - Implement metrics collection
   - Create health checks
   - Set up alerts

3. **Testing**
   - Unit tests for each stage
   - Integration tests
   - Load testing
   - Failure scenario testing

**Estimated Effort:** 40 hours

### Total Estimated Effort: 220 hours (5.5 weeks for one developer)

---

## Scalability Considerations

### Handling 100+ Concurrent Videos

**Horizontal Scaling Strategy:**

1. **Multiple Hangfire Servers**
   ```csharp
   // Deploy multiple instances with:
   builder.Services.AddHangfireServer(options =>
   {
       options.ServerName = $"Server-{Environment.MachineName}";
       options.WorkerCount = Environment.ProcessorCount * 2;
       options.Queues = new[] { "critical", "default", "low" };
   });
   ```

2. **Queue Partitioning**
   - Separate queues by priority
   - Dedicate servers to specific queues
   - Use queue-based load balancing

3. **Resource Pooling**
   ```csharp
   public class ResourcePool<T>
   {
       private readonly ConcurrentBag<T> _resources;
       private readonly SemaphoreSlim _semaphore;

       public async Task<T> AcquireAsync()
       {
           await _semaphore.WaitAsync();
           return _resources.TryTake(out var resource)
               ? resource
               : await CreateResourceAsync();
       }
   }
   ```

4. **Database Optimization**
   - Connection pooling
   - Read replicas for queries
   - Batch inserts for segments
   - Indexed columns for job queries

### Performance Optimization

1. **Caching Strategy**
   - Cache YouTube metadata (TTL: 24 hours)
   - Cache embedding model in memory
   - Use Redis for distributed caching

2. **Batch Processing**
   - Batch embedding generation (10-20 segments)
   - Bulk database inserts
   - Parallel stage execution where possible

3. **Resource Management**
   ```csharp
   public class ResourceGovernor
   {
       private readonly SemaphoreSlim _cpuSemaphore;
       private readonly SemaphoreSlim _memorySemaphore;
       private readonly SemaphoreSlim _ioSemaphore;

       public async Task<IDisposable> AcquireResourcesAsync(ResourceRequirements requirements)
       {
           // Acquire resources based on requirements
           // Return disposable to release when done
       }
   }
   ```

4. **Auto-Scaling Rules**
   - Scale up when queue depth > 50
   - Scale down when idle for 10 minutes
   - Max instances: 10
   - Min instances: 2

---

## Testing Strategy

### Unit Testing

```csharp
[TestClass]
public class MetadataExtractionStageTests
{
    [TestMethod]
    public async Task ExecuteAsync_ValidUrl_ReturnsMetadata()
    {
        // Arrange
        var stage = new MetadataExtractionStage(_mockYouTubeService);
        var context = new StageContext { /* ... */ };

        // Act
        var result = await stage.ExecuteAsync(context);

        // Assert
        Assert.IsNotNull(result.Output);
        Assert.AreEqual(StageStatus.Completed, result.Status);
    }

    [TestMethod]
    public async Task ExecuteAsync_InvalidUrl_ThrowsValidationException()
    {
        // Test invalid URL handling
    }

    [TestMethod]
    public async Task ExecuteAsync_RateLimit_RetriesWithBackoff()
    {
        // Test retry logic
    }
}
```

### Integration Testing

```csharp
[TestClass]
public class VideoIngestionPipelineIntegrationTests
{
    [TestMethod]
    public async Task Pipeline_CompleteFlow_ProcessesVideoSuccessfully()
    {
        // Arrange
        var pipeline = new VideoIngestionPipeline();
        var request = new VideoIngestionRequest
        {
            YouTubeUrl = "https://youtube.com/watch?v=test",
            UserId = "test-user"
        };

        // Act
        var jobId = await _ingestionService.StartIngestionAsync(request);
        await WaitForCompletionAsync(jobId, TimeSpan.FromMinutes(5));

        // Assert
        var job = await _jobService.GetJobAsync(jobId);
        Assert.AreEqual(JobStatus.Completed, job.Status);
        Assert.IsTrue(job.Video.TranscriptSegments.Count > 0);
    }
}
```

### Load Testing

```csharp
[TestClass]
public class LoadTests
{
    [TestMethod]
    public async Task Pipeline_100ConcurrentVideos_CompletesWithin30Minutes()
    {
        // Arrange
        var tasks = new List<Task<string>>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(_ingestionService.StartIngestionAsync(CreateRequest(i)));
        }

        var jobIds = await Task.WhenAll(tasks);
        var completionTasks = jobIds.Select(id =>
            WaitForCompletionAsync(id, TimeSpan.FromMinutes(30)));

        // Assert
        var results = await Task.WhenAll(completionTasks);
        Assert.IsTrue(results.All(r => r.Status == JobStatus.Completed));
    }
}
```

### Mocking Strategy

```csharp
public class MockYouTubeService : IYouTubeService
{
    public async Task<VideoMetadata> GetVideoMetadataAsync(string videoId)
    {
        // Return test data based on videoId
        return new VideoMetadata
        {
            Title = $"Test Video {videoId}",
            Duration = TimeSpan.FromMinutes(5),
            // ...
        };
    }
}

public class MockTranscriptionService : ITranscriptionService
{
    public async Task<TranscriptionResult> TranscribeAsync(Stream audioStream, TranscriptionOptions options)
    {
        // Generate synthetic transcription for testing
        return new TranscriptionResult
        {
            Segments = GenerateTestSegments(),
            Language = "en",
            Confidence = 0.95
        };
    }
}
```

---

## Risk Assessment & Mitigation

| Risk | Impact | Probability | Mitigation Strategy |
|------|--------|-------------|-------------------|
| YouTube API Rate Limits | High | Medium | Implement caching, rate limiting, exponential backoff |
| **YouTube 403 Blocking** | **High** | **High** | **✅ IMPLEMENTED: yt-dlp automatic fallback on 403 errors** |
| Whisper Service Timeout | High | Medium | ✅ IMPLEMENTED: 60-minute timeout with process kill |
| Database Connection Loss | High | Low | Connection retry logic, connection pooling, health checks |
| Memory Exhaustion | High | Medium | Resource governor, memory limits, disposal patterns |
| Concurrent Job Overload | Medium | High | Queue throttling, auto-scaling, priority queues |
| Network Failures | Medium | Medium | Retry policies, circuit breakers, timeout handling |
| Data Corruption | High | Low | Transaction management, validation, audit logging |

---

## Open Questions & Trade-offs

### Questions Requiring Clarification

1. **Storage Strategy for Embeddings**
   - Store in MySQL as JSON or separate vector database?
   - Trade-off: Simplicity vs. specialized vector search performance

2. **Video File Storage**
   - Keep original audio files after processing?
   - Trade-off: Storage costs vs. reprocessing capability

3. **Processing Priority Logic**
   - FIFO, user-tier based, or video-length based?
   - Trade-off: Fairness vs. resource optimization

### Technical Trade-offs

1. **Hangfire vs. Azure Functions**
   - Hangfire: Self-hosted, more control, integrated dashboard
   - Azure Functions: Managed, auto-scaling, higher cost
   - **Recommendation:** Hangfire for on-premise/control

2. **SignalR vs. Polling**
   - SignalR: Real-time, efficient, complex infrastructure
   - Polling: Simple, compatible, higher latency
   - **Recommendation:** SignalR for better UX

3. **Local vs. Cloud Processing**
   - Local: Lower cost, more control, resource constraints
   - Cloud: Scalable, managed, higher cost
   - **Recommendation:** Hybrid with configuration toggle

### Future Considerations

1. **Multi-language Support**
   - Implement language detection
   - Support for non-English embeddings
   - Multilingual search capabilities

2. **Video Chunking for Large Files**
   - Process videos > 1 hour in chunks
   - Parallel processing of chunks
   - Chunk reassembly logic

3. **Advanced Features**
   - Speaker diarization
   - Sentiment analysis
   - Topic modeling
   - Key phrase extraction

---

## Conclusion

This architecture provides a robust, scalable foundation for video ingestion and processing in the YouTube RAG application. The design emphasizes:

- **Modularity** through pipeline stages
- **Resilience** through retry and circuit breaker patterns
- **Scalability** through queue-based processing and horizontal scaling
- **Observability** through comprehensive logging and real-time progress tracking
- **Flexibility** through configuration-driven processing modes

The implementation plan provides a clear path forward with measurable milestones and realistic effort estimates. The architecture is designed to handle current requirements while remaining extensible for future enhancements.