using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Exceptions;
using YoutubeRag.Application.DTOs.Video;
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
    private readonly AsyncRetryPolicy<VideoMetadataDto> _retryPolicy;

    public MetadataExtractionService(ILogger<MetadataExtractionService> logger)
    {
        _logger = logger;
        _youtubeClient = new YoutubeClient();
        _retryPolicy = PollyPolicies.CreateMetadataExtractionPolicy<VideoMetadataDto>(logger, maxRetries: 3);
    }

    /// <inheritdoc />
    public async Task<VideoMetadataDto> ExtractMetadataAsync(string youTubeId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(youTubeId))
        {
            throw new ArgumentException("YouTube ID cannot be null or empty", nameof(youTubeId));
        }

        _logger.LogInformation("Extracting metadata for YouTube video: {YouTubeId}", youTubeId);

        // Create Polly context with videoId for logging
        var context = new Context("ExtractMetadata");
        context["videoId"] = youTubeId;

        return await _retryPolicy.ExecuteAsync(async (ctx, ct) =>
        {
            try
            {
                // Get video metadata from YouTube
                var video = await _youtubeClient.Videos.GetAsync(youTubeId, ct);

                // Get video manifest for additional details (optional, for more comprehensive data)
                var manifest = await _youtubeClient.Videos.Streams.GetManifestAsync(youTubeId, ct);

                var metadata = new VideoMetadataDto
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
                    Tags = video.Keywords.ToList()
                };

                _logger.LogInformation(
                    "Successfully extracted metadata for video: {Title} (ID: {YouTubeId}, Duration: {Duration}, Views: {Views})",
                    metadata.Title,
                    youTubeId,
                    metadata.Duration,
                    metadata.ViewCount
                );

                return metadata;
            }
            catch (VideoUnavailableException ex)
            {
                _logger.LogWarning(ex, "Video {YouTubeId} is unavailable (private, deleted, or region-blocked)", youTubeId);
                throw new InvalidOperationException($"The video '{youTubeId}' is not available. It may be private, deleted, or region-blocked.", ex);
            }
            catch (VideoUnplayableException ex)
            {
                _logger.LogWarning(ex, "Video {YouTubeId} is unplayable", youTubeId);
                throw new InvalidOperationException($"The video '{youTubeId}' is unplayable.", ex);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogWarning(ex, "YoutubeExplode failed with 403 Forbidden. Attempting fallback to yt-dlp for video: {YouTubeId}", youTubeId);

                // Fallback to yt-dlp (not retried by Polly)
                return await ExtractMetadataUsingYtDlpAsync(youTubeId, ct);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Metadata extraction was cancelled for video {YouTubeId}", youTubeId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while extracting metadata for video {YouTubeId}", youTubeId);
                throw new InvalidOperationException($"An unexpected error occurred while extracting metadata for video '{youTubeId}'.", ex);
            }
        }, context, cancellationToken);
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
                    : new List<string>()
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