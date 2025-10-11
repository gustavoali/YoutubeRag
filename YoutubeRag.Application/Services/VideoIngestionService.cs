using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using YoutubeRag.Application.Configuration;
using YoutubeRag.Application.DTOs.Video;
using YoutubeRag.Application.Exceptions;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Application.Interfaces.Services;
using YoutubeRag.Application.Utilities;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Application.Services;

public class VideoIngestionService : IVideoIngestionService
{
    private readonly IVideoRepository _videoRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IYouTubeService _youTubeService;
    private readonly IMetadataExtractionService _metadataExtractionService;
    private readonly IAppConfiguration _appConfiguration;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly ILogger<VideoIngestionService> _logger;

    // Regex patterns for YouTube URL parsing
    private static readonly Regex YouTubeIdRegex = new(
        @"(?:youtube\.com\/watch\?v=|youtu\.be\/|youtube\.com\/embed\/|youtube\.com\/v\/)([a-zA-Z0-9_-]{11})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public VideoIngestionService(
        IVideoRepository videoRepository,
        IJobRepository jobRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IYouTubeService youTubeService,
        IMetadataExtractionService metadataExtractionService,
        IAppConfiguration appConfiguration,
        IBackgroundJobService backgroundJobService,
        ILogger<VideoIngestionService> logger)
    {
        _videoRepository = videoRepository;
        _jobRepository = jobRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _youTubeService = youTubeService;
        _metadataExtractionService = metadataExtractionService;
        _appConfiguration = appConfiguration;
        _backgroundJobService = backgroundJobService;
        _logger = logger;
    }

    public async Task<VideoIngestionResponse> IngestVideoFromUrlAsync(
        VideoIngestionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting video ingestion from URL: {Url} for user: {UserId} with priority: {Priority}",
            request.Url, request.UserId, request.Priority);

        // Step 1: Validate and extract YouTube ID
        var (isValid, youTubeId, errorMessage) = await ValidateYouTubeUrlAsync(request.Url, cancellationToken);

        if (!isValid || string.IsNullOrEmpty(youTubeId))
        {
            _logger.LogWarning("Invalid YouTube URL: {Url} - {Error}", request.Url, errorMessage);
            throw new BusinessValidationException($"Invalid YouTube URL: {errorMessage}");
        }

        // Step 2: Check if video already exists
        var existingVideo = await _videoRepository.GetByYouTubeIdAsync(youTubeId, cancellationToken);
        if (existingVideo != null)
        {
            _logger.LogInformation("Video already exists: {YouTubeId} - VideoId: {VideoId}",
                youTubeId, existingVideo.Id);
            throw new DuplicateResourceException("Video", existingVideo.Id);
        }

        // Step 3: Extract metadata from YouTube
        VideoMetadataDto? metadata = null;
        try
        {
            var metadataStartTime = DateTime.UtcNow;
            _logger.LogInformation("Extracting metadata for YouTube video: {YouTubeId}", youTubeId);
            metadata = await _metadataExtractionService.ExtractMetadataAsync(youTubeId, cancellationToken);
            var metadataDuration = (DateTime.UtcNow - metadataStartTime).TotalMilliseconds;
            _logger.LogInformation("Metadata extraction completed for {YouTubeId} in {DurationMs}ms. Duration: {VideoDuration}, Views: {ViewCount}",
                youTubeId, metadataDuration, metadata?.Duration, metadata?.ViewCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract metadata for video {YouTubeId}. Continuing with basic info.", youTubeId);
        }

        // Step 3.5: Ensure user exists (auto-create in Local/Development environments)
        await EnsureUserExistsAsync(request.UserId, cancellationToken);

        // Step 4: Create Video entity with metadata
        var video = new Video
        {
            Id = Guid.NewGuid().ToString(),
            YouTubeId = youTubeId,
            Title = request.Title ?? metadata?.Title ?? $"Video {youTubeId}",
            Description = request.Description ?? metadata?.Description,
            Duration = metadata?.Duration,
            ViewCount = metadata?.ViewCount,
            LikeCount = metadata?.LikeCount,
            PublishedAt = metadata?.PublishedAt,
            ChannelId = metadata?.ChannelId,
            ChannelTitle = metadata?.ChannelTitle,
            CategoryId = metadata?.CategoryId,
            Tags = metadata?.Tags ?? new List<string>(),
            ThumbnailUrl = metadata?.ThumbnailUrls?.FirstOrDefault(),
            UserId = request.UserId,
            Status = VideoStatus.Pending,
            Url = request.Url,
            OriginalUrl = request.Url,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Step 5: Create initial Job for processing
        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = video.Id,
            UserId = request.UserId,
            Type = JobType.VideoProcessing,
            Status = JobStatus.Pending,
            Priority = (int)request.Priority,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Step 6: Save to database
        await _videoRepository.AddAsync(video);
        await _jobRepository.AddAsync(job);

        // Step 7: Save video and initial job first
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateException")
        {
            _logger.LogError(ex, "Failed to save video and job to database for YouTube ID: {YouTubeId}", youTubeId);
            throw new DatabaseException("Failed to save video to database", ex);
        }

        // Step 8: Create transcription job if auto-transcribe is enabled
        Job? transcriptionJob = null;
        string? hangfireJobId = null;

        if (_appConfiguration.AutoTranscribe)
        {
            transcriptionJob = new Job
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = video.Id,
                UserId = request.UserId,
                Type = JobType.Transcription,
                Status = JobStatus.Pending,
                Priority = (int)request.Priority,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    VideoTitle = video.Title,
                    YouTubeId = video.YouTubeId,
                    Language = "auto"
                })
            };

            // Enqueue the transcription job with Hangfire
            hangfireJobId = _backgroundJobService.EnqueueTranscriptionJob(
                video.Id,
                MapProcessingPriorityToJobPriority(request.Priority));

            // Store the Hangfire job ID
            transcriptionJob.HangfireJobId = hangfireJobId;

            await _jobRepository.AddAsync(transcriptionJob);
            video.TranscriptionStatus = TranscriptionStatus.Pending;

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex) when (ex.GetType().Name == "DbUpdateException")
            {
                _logger.LogError(ex, "Failed to save transcription job to database for video: {VideoId}", video.Id);
                throw new DatabaseException("Failed to save transcription job to database", ex);
            }

            _logger.LogInformation("Created and enqueued transcription job for video: {VideoId}, JobId: {JobId}, HangfireJobId: {HangfireJobId}, Priority: {Priority}",
                video.Id, transcriptionJob.Id, hangfireJobId, request.Priority);
        }

        var totalDuration = (DateTime.UtcNow - startTime).TotalMilliseconds;
        _logger.LogInformation("Video ingestion completed successfully in {DurationMs}ms. VideoId: {VideoId}, JobId: {JobId}, Title: {Title}, TranscriptionJobId: {TranscriptionJobId}, UserId: {UserId}",
            totalDuration, video.Id, job.Id, video.Title, transcriptionJob?.Id ?? "N/A", request.UserId);

        return new VideoIngestionResponse
        {
            VideoId = video.Id,
            JobId = job.Id,
            YouTubeId = youTubeId,
            Status = VideoStatus.Pending.ToString(),
            Message = "Video ingestion initiated successfully",
            SubmittedAt = video.CreatedAt,
            ProgressUrl = $"/api/v1/videos/{video.Id}/progress"
        };
    }

    public async Task<(bool IsValid, string? YouTubeId, string? ErrorMessage)> ValidateYouTubeUrlAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return (false, null, "URL cannot be empty");
        }

        // Extract YouTube ID
        var youTubeId = ExtractYouTubeId(url);
        if (string.IsNullOrEmpty(youTubeId))
        {
            return (false, null, "Could not extract YouTube video ID from URL");
        }

        // Validate ID format (11 characters, alphanumeric + - and _)
        if (youTubeId.Length != 11)
        {
            return (false, null, "Invalid YouTube video ID format");
        }

        // Check if video is accessible using metadata extraction service
        // Note: This is a soft check - we log warnings but don't reject the video
        // The video might be accessible via yt-dlp fallback even if YoutubeExplode fails
        try
        {
            var isAccessible = await _metadataExtractionService.IsVideoAccessibleAsync(youTubeId, cancellationToken);
            if (!isAccessible)
            {
                _logger.LogWarning("Video {YouTubeId} may not be accessible via YouTube API, but will attempt ingestion with fallback", youTubeId);
                // Continue anyway - metadata extraction will handle fallback to yt-dlp if needed
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not verify video accessibility for {YouTubeId}", youTubeId);
            // Continue anyway - let it fail later if the video is truly inaccessible
        }

        return (true, youTubeId, null);
    }

    public string? ExtractYouTubeId(string url)
    {
        return YouTubeUrlParser.ExtractVideoId(url);
    }

    public async Task<bool> IsVideoAlreadyIngestedAsync(
        string youTubeId,
        CancellationToken cancellationToken = default)
    {
        var video = await _videoRepository.GetByYouTubeIdAsync(youTubeId, cancellationToken);
        return video != null;
    }

    private static JobPriority MapProcessingPriorityToJobPriority(ProcessingPriority processingPriority)
    {
        return processingPriority switch
        {
            ProcessingPriority.Low => JobPriority.Low,
            ProcessingPriority.Normal => JobPriority.Normal,
            ProcessingPriority.High => JobPriority.High,
            ProcessingPriority.Critical => JobPriority.Critical,
            _ => JobPriority.Normal
        };
    }

    /// <summary>
    /// Ensures the specified user exists in the database.
    /// In Local/Development environments, automatically creates a test user if not found.
    /// In Production, throws an exception if the user doesn't exist.
    /// </summary>
    /// <param name="userId">The user identifier to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="InvalidOperationException">Thrown when user doesn't exist in Production environment</exception>
    private async Task EnsureUserExistsAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("UserId is null or empty. Skipping user existence check.");
            return;
        }

        var existingUser = await _userRepository.GetByIdAsync(userId);
        if (existingUser != null)
        {
            _logger.LogDebug("User {UserId} exists in database", userId);
            return;
        }

        // User doesn't exist - check environment
        var environment = _appConfiguration.Environment;
        var isLocalOrDev = environment.Equals("Local", StringComparison.OrdinalIgnoreCase) ||
                          environment.Equals("Development", StringComparison.OrdinalIgnoreCase);

        if (!isLocalOrDev)
        {
            _logger.LogError("User {UserId} not found in {Environment} environment", userId, environment);
            throw new InvalidOperationException($"User {userId} not found");
        }

        // Auto-create test user in Local/Development environments
        _logger.LogWarning("User {UserId} not found. Creating test user for {Environment} environment",
            userId, environment);

        var testUser = new User
        {
            Id = userId,
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = "test-hash-not-used-in-mock-auth",
            IsActive = true,
            IsEmailVerified = true,
            EmailVerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(testUser);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateException")
        {
            _logger.LogError(ex, "Failed to create test user {UserId} in database", userId);
            throw new DatabaseException("Failed to create test user in database", ex);
        }

        _logger.LogInformation("Successfully created test user {UserId} with email {Email} for local testing",
            userId, testUser.Email);
    }
}
