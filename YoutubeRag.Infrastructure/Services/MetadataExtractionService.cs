using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Exceptions;
using YoutubeRag.Application.DTOs.Video;
using YoutubeRag.Application.Exceptions;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Infrastructure.Resilience;
using Polly;
using Polly.Retry;

namespace YoutubeRag.Infrastructure.Services;

/// <summary>
/// Service implementation for extracting metadata from YouTube videos using YoutubeExplode
/// </summary>
public class MetadataExtractionService : IMetadataExtractionService
{
    private readonly YoutubeClient _youtubeClient;
    private readonly ILogger<MetadataExtractionService> _logger;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly AsyncRetryPolicy<VideoMetadataDto> _retryPolicy;

    private const int DefaultTimeoutSeconds = 30;
    private const int DefaultMaxRetries = 3;
    private const int DefaultMaxVideoDurationSeconds = 14400; // 4 hours
    private const int DefaultMetadataCacheDurationMinutes = 5;

    public MetadataExtractionService(
        ILogger<MetadataExtractionService> logger,
        IMemoryCache cache,
        IConfiguration configuration)
    {
        _logger = logger;
        _cache = cache;
        _configuration = configuration;
        _youtubeClient = new YoutubeClient();

        var maxRetries = _configuration.GetValue<int?>("YouTube:MaxRetries") ?? DefaultMaxRetries;
        _retryPolicy = PollyPolicies.CreateMetadataExtractionPolicy<VideoMetadataDto>(logger, maxRetries);
    }

    /// <inheritdoc />
    public async Task<VideoMetadataDto> ExtractMetadataAsync(string youTubeId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(youTubeId))
        {
            throw new ArgumentException("YouTube ID cannot be null or empty", nameof(youTubeId));
        }

        // Check cache first (AC5: Cachear metadata por 5 minutos)
        string cacheKey = $"metadata_{youTubeId}";
        if (_cache.TryGetValue(cacheKey, out VideoMetadataDto? cachedMetadata))
        {
            _logger.LogInformation("Metadata cache hit for video: {YouTubeId}", youTubeId);
            return cachedMetadata!;
        }

        _logger.LogInformation("Extracting metadata for YouTube video: {YouTubeId}", youTubeId);

        // Get timeout configuration
        var timeoutSeconds = _configuration.GetValue<int?>("YouTube:TimeoutSeconds") ?? DefaultTimeoutSeconds;

        // Create timeout cancellation token
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        // Create Polly context with videoId for logging
        var context = new Context("ExtractMetadata");
        context["videoId"] = youTubeId;

        var metadata = await _retryPolicy.ExecuteAsync(async (ctx, ct) =>
        {
            try
            {
                // Get video metadata from YouTube
                var video = await _youtubeClient.Videos.GetAsync(youTubeId, ct);

                // Extract metadata
                var extractedMetadata = new VideoMetadataDto
                {
                    Title = video.Title,
                    Description = video.Description,
                    Duration = video.Duration,
                    ViewCount = video.Engagement?.ViewCount is null ? null : (int?)video.Engagement.ViewCount,
                    LikeCount = video.Engagement?.LikeCount is null ? null : (int?)video.Engagement.LikeCount,
                    PublishedAt = video.UploadDate.DateTime,
                    ChannelId = video.Author?.ChannelId.Value,
                    ChannelTitle = video.Author?.ChannelTitle,
                    ThumbnailUrls = GetThumbnailUrls(video.Thumbnails),
                    Tags = video.Keywords.ToList(),
                    CategoryId = null // YoutubeExplode doesn't provide category, but we keep the field for yt-dlp fallback
                };

                // AC3: Validación de Metadata
                ValidateMetadata(extractedMetadata, youTubeId);

                _logger.LogInformation(
                    "Successfully extracted metadata for video: {Title} (ID: {YouTubeId}, Duration: {Duration}, Views: {Views})",
                    extractedMetadata.Title,
                    youTubeId,
                    extractedMetadata.Duration,
                    extractedMetadata.ViewCount
                );

                return extractedMetadata;
            }
            catch (VideoUnavailableException ex)
            {
                // AC4: Manejo de Videos No Disponibles
                var errorMessage = ex.Message.ToLowerInvariant();

                if (errorMessage.Contains("private"))
                {
                    _logger.LogWarning(ex, "Video {YouTubeId} is private", youTubeId);
                    throw new BusinessValidationException("VIDEO_PRIVATE", "This video is private and cannot be accessed");
                }

                if (errorMessage.Contains("deleted") || errorMessage.Contains("removed"))
                {
                    _logger.LogWarning(ex, "Video {YouTubeId} has been deleted", youTubeId);
                    throw new BusinessValidationException("VIDEO_DELETED", "This video has been deleted or removed");
                }

                _logger.LogWarning(ex, "Video {YouTubeId} is unavailable (possibly region-blocked)", youTubeId);
                throw new BusinessValidationException("VIDEO_UNAVAILABLE", "This video is not available. It may be region-blocked or restricted");
            }
            catch (VideoUnplayableException ex)
            {
                // Check for age restrictions
                var errorMessage = ex.Message.ToLowerInvariant();
                if (errorMessage.Contains("age") || errorMessage.Contains("restricted"))
                {
                    _logger.LogWarning(ex, "Video {YouTubeId} has age restrictions", youTubeId);
                    throw new BusinessValidationException("VIDEO_AGE_RESTRICTED", "This video has age restrictions and cannot be accessed");
                }

                _logger.LogWarning(ex, "Video {YouTubeId} is unplayable", youTubeId);
                throw new BusinessValidationException("VIDEO_UNPLAYABLE", "This video is unplayable");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogWarning(ex, "YoutubeExplode failed with 403 Forbidden. Attempting fallback to yt-dlp for video: {YouTubeId}", youTubeId);

                // Fallback to yt-dlp (not retried by Polly)
                var fallbackMetadata = await ExtractMetadataUsingYtDlpAsync(youTubeId, ct);
                ValidateMetadata(fallbackMetadata, youTubeId);
                return fallbackMetadata;
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout occurred
                _logger.LogWarning(ex, "Metadata extraction timed out for video {YouTubeId} after {Timeout} seconds", youTubeId, timeoutSeconds);
                throw new BusinessValidationException("EXTRACTION_TIMEOUT", $"Metadata extraction timed out after {timeoutSeconds} seconds");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Metadata extraction was cancelled for video {YouTubeId}", youTubeId);
                throw;
            }
            catch (BusinessValidationException)
            {
                // Re-throw business validation exceptions
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while extracting metadata for video {YouTubeId}", youTubeId);
                throw new InvalidOperationException($"An unexpected error occurred while extracting metadata for video '{youTubeId}'.", ex);
            }
        }, context, timeoutCts.Token);

        // Cache metadata for configured duration (AC5: Cachear metadata por 5 minutos)
        var cacheDurationMinutes = _configuration.GetValue<int?>("YouTube:MetadataCacheDurationMinutes") ?? DefaultMetadataCacheDurationMinutes;
        _cache.Set(cacheKey, metadata, TimeSpan.FromMinutes(cacheDurationMinutes));
        _logger.LogDebug("Cached metadata for video {YouTubeId} for {Duration} minutes", youTubeId, cacheDurationMinutes);

        return metadata;
    }

    /// <summary>
    /// Validates extracted metadata according to business rules (AC3)
    /// </summary>
    private void ValidateMetadata(VideoMetadataDto metadata, string youTubeId)
    {
        var maxDuration = _configuration.GetValue<int?>("YouTube:MaxVideoDurationSeconds") ?? DefaultMaxVideoDurationSeconds;

        // Duración > 0
        if (metadata.DurationSeconds <= 0)
        {
            _logger.LogWarning("Video {YouTubeId} has invalid duration: {Duration}", youTubeId, metadata.Duration);
            throw new BusinessValidationException("INVALID_DURATION", "Video duration must be greater than 0");
        }

        // Duración < 14400 segundos (4 horas max)
        if (metadata.DurationSeconds > maxDuration)
        {
            var maxHours = maxDuration / 3600;
            _logger.LogWarning("Video {YouTubeId} exceeds maximum duration: {Duration} (max: {MaxHours} hours)",
                youTubeId, metadata.Duration, maxHours);
            throw new BusinessValidationException("VIDEO_TOO_LONG",
                $"Video exceeds maximum duration of {maxHours} hours (video is {metadata.Duration?.TotalHours:F2} hours)");
        }

        // Título no vacío
        if (string.IsNullOrWhiteSpace(metadata.Title))
        {
            _logger.LogWarning("Video {YouTubeId} has empty title", youTubeId);
            throw new BusinessValidationException("INVALID_TITLE", "Video title cannot be empty");
        }

        // Thumbnail URL válida
        if (string.IsNullOrWhiteSpace(metadata.ThumbnailUrl))
        {
            _logger.LogWarning("Video {YouTubeId} has no thumbnail URL", youTubeId);
            throw new BusinessValidationException("INVALID_THUMBNAIL", "Video must have a valid thumbnail URL");
        }

        // Warning si faltan campos opcionales
        if (metadata.ViewCount == null)
        {
            _logger.LogWarning("Video {YouTubeId} is missing ViewCount", youTubeId);
        }

        if (string.IsNullOrWhiteSpace(metadata.Description))
        {
            _logger.LogWarning("Video {YouTubeId} is missing Description", youTubeId);
        }

        if (metadata.Tags == null || !metadata.Tags.Any())
        {
            _logger.LogWarning("Video {YouTubeId} has no tags", youTubeId);
        }

        if (string.IsNullOrWhiteSpace(metadata.CategoryId))
        {
            _logger.LogDebug("Video {YouTubeId} is missing CategoryId (normal for YoutubeExplode)", youTubeId);
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsVideoAccessibleAsync(string youTubeId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(youTubeId))
        {
            return false;
        }

        _logger.LogDebug("Checking accessibility for YouTube video: {YouTubeId}", youTubeId);

        try
        {
            // Try to get video metadata - if successful, video is accessible
            var video = await _youtubeClient.Videos.GetAsync(youTubeId, cancellationToken);

            _logger.LogDebug("Video {YouTubeId} is accessible", youTubeId);
            return true;
        }
        catch (VideoUnavailableException)
        {
            _logger.LogDebug("Video {YouTubeId} is not accessible (unavailable)", youTubeId);
            return false;
        }
        catch (VideoUnplayableException)
        {
            _logger.LogDebug("Video {YouTubeId} is not accessible (unplayable)", youTubeId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking accessibility for video {YouTubeId}", youTubeId);
            return false;
        }
    }

    /// <summary>
    /// Extracts thumbnail URLs from the thumbnail collection
    /// </summary>
    private List<string> GetThumbnailUrls(IReadOnlyList<Thumbnail> thumbnails)
    {
        if (thumbnails == null || !thumbnails.Any())
        {
            return new List<string>();
        }

        // Sort by resolution (highest first) and get unique URLs
        return thumbnails
            .OrderByDescending(t => t.Resolution.Area)
            .Select(t => t.Url)
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Finds the yt-dlp executable in common locations
    /// </summary>
    private string FindYtDlpExecutable()
    {
        // Common yt-dlp locations
        var possiblePaths = new[]
        {
            "yt-dlp",
            "yt-dlp.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "Local", "Packages", "PythonSoftwareFoundation.Python.3.13_qbz5n2kfra8p0", "LocalCache", "local-packages", "Python313", "Scripts", "yt-dlp.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Python", "Python313", "Scripts", "yt-dlp.exe"),
            "/usr/local/bin/yt-dlp",
            "/usr/bin/yt-dlp"
        };

        foreach (var path in possiblePaths)
        {
            try
            {
                // For absolute paths with .exe extension, check if file exists directly
                if (Path.IsPathRooted(path) && path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    if (File.Exists(path))
                    {
                        _logger.LogInformation("Found yt-dlp executable at: {Path}", path);
                        return path;
                    }
                    continue;
                }

                // For simple commands, try to execute
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.WaitForExit(2000);
                if (process.ExitCode == 0)
                {
                    _logger.LogInformation("Found yt-dlp executable at: {Path}", path);
                    return path;
                }
            }
            catch
            {
                // Continue searching
            }
        }

        _logger.LogWarning("yt-dlp executable not found. Install with: pip install yt-dlp");
        return string.Empty;
    }

    /// <summary>
    /// Extracts metadata using yt-dlp as a fallback when YoutubeExplode fails
    /// </summary>
    private async Task<VideoMetadataDto> ExtractMetadataUsingYtDlpAsync(string youTubeId, CancellationToken cancellationToken)
    {
        var ytDlpPath = FindYtDlpExecutable();
        if (string.IsNullOrEmpty(ytDlpPath))
        {
            throw new InvalidOperationException("yt-dlp is not available. Please install it with: pip install yt-dlp");
        }

        try
        {
            _logger.LogInformation("Using yt-dlp to extract metadata for video: {YouTubeId}", youTubeId);

            var videoUrl = $"https://www.youtube.com/watch?v={youTubeId}";
            var arguments = $"--dump-json --no-download \"{videoUrl}\"";

            var processInfo = new ProcessStartInfo
            {
                FileName = ytDlpPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            // Set environment variables for Unicode support
            processInfo.Environment["PYTHONIOENCODING"] = "utf-8";
            processInfo.Environment["PYTHONUTF8"] = "1";

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start yt-dlp process");
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            // Wait with timeout (30 seconds for metadata extraction)
            var timeout = TimeSpan.FromSeconds(30);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError("yt-dlp process timed out after {Timeout} seconds", timeout.TotalSeconds);
                if (!process.HasExited)
                {
                    process.Kill();
                    await process.WaitForExitAsync();
                }
                throw new TimeoutException($"yt-dlp metadata extraction timed out after {timeout.TotalSeconds} seconds for video {youTubeId}");
            }

            if (process.ExitCode != 0)
            {
                _logger.LogError("yt-dlp failed with exit code {ExitCode}. Error: {Error}", process.ExitCode, error);
                throw new InvalidOperationException($"yt-dlp failed with exit code {process.ExitCode}: {error}");
            }

            // Parse JSON output
            using var jsonDoc = JsonDocument.Parse(output);
            var root = jsonDoc.RootElement;

            // Extract metadata from yt-dlp JSON
            var metadata = new VideoMetadataDto
            {
                Title = root.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? string.Empty : string.Empty,
                Description = root.TryGetProperty("description", out var descProp) ? descProp.GetString() : null,
                Duration = root.TryGetProperty("duration", out var durationProp) && durationProp.TryGetDouble(out var durationSeconds)
                    ? TimeSpan.FromSeconds(durationSeconds)
                    : null,
                ChannelTitle = root.TryGetProperty("channel", out var channelProp) ? channelProp.GetString() : null,
                ChannelId = root.TryGetProperty("channel_id", out var channelIdProp) ? channelIdProp.GetString() : null,
                ViewCount = root.TryGetProperty("view_count", out var viewsProp) && viewsProp.TryGetInt64(out var views)
                    ? (int?)views
                    : null,
                LikeCount = root.TryGetProperty("like_count", out var likesProp) && likesProp.TryGetInt64(out var likes)
                    ? (int?)likes
                    : null,
                PublishedAt = ParseUploadDate(root),
                ThumbnailUrls = ExtractThumbnailUrls(root),
                Tags = root.TryGetProperty("tags", out var tagsProp) && tagsProp.ValueKind == JsonValueKind.Array
                    ? tagsProp.EnumerateArray().Select(t => t.GetString()).Where(t => t != null).ToList()!
                    : new List<string>(),
                CategoryId = root.TryGetProperty("category_id", out var categoryIdProp) ? categoryIdProp.GetString() : null
            };

            _logger.LogInformation(
                "Successfully extracted metadata using yt-dlp for video: {Title} (ID: {YouTubeId}, Duration: {Duration}, Views: {Views})",
                metadata.Title,
                youTubeId,
                metadata.Duration,
                metadata.ViewCount
            );

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract metadata using yt-dlp for video: {YouTubeId}", youTubeId);
            throw;
        }
    }

    /// <summary>
    /// Parses the upload date from yt-dlp JSON output (format: YYYYMMDD)
    /// </summary>
    private DateTime? ParseUploadDate(JsonElement root)
    {
        if (!root.TryGetProperty("upload_date", out var uploadDateProp))
        {
            return null;
        }

        var uploadDateStr = uploadDateProp.GetString();
        if (string.IsNullOrEmpty(uploadDateStr) || uploadDateStr.Length != 8)
        {
            return null;
        }

        try
        {
            var year = int.Parse(uploadDateStr.Substring(0, 4));
            var month = int.Parse(uploadDateStr.Substring(4, 2));
            var day = int.Parse(uploadDateStr.Substring(6, 2));
            return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
        }
        catch
        {
            _logger.LogWarning("Failed to parse upload date: {UploadDate}", uploadDateStr);
            return null;
        }
    }

    /// <summary>
    /// Extracts thumbnail URLs from yt-dlp JSON output
    /// </summary>
    private List<string> ExtractThumbnailUrls(JsonElement root)
    {
        var thumbnailUrls = new List<string>();

        // Try to get thumbnail URL (yt-dlp provides single best thumbnail URL)
        if (root.TryGetProperty("thumbnail", out var thumbnailProp) && thumbnailProp.GetString() is string thumbnailUrl)
        {
            thumbnailUrls.Add(thumbnailUrl);
        }

        // Also check for thumbnails array if available
        if (root.TryGetProperty("thumbnails", out var thumbnailsProp) && thumbnailsProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var thumb in thumbnailsProp.EnumerateArray())
            {
                if (thumb.TryGetProperty("url", out var urlProp) && urlProp.GetString() is string url)
                {
                    if (!thumbnailUrls.Contains(url))
                    {
                        thumbnailUrls.Add(url);
                    }
                }
            }
        }

        return thumbnailUrls;
    }
}