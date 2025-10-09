using Microsoft.Extensions.Logging;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using YoutubeRag.Application.Interfaces;

namespace YoutubeRag.Infrastructure.Services;

/// <summary>
/// Service for downloading YouTube videos with progress tracking and disk space management
/// </summary>
public class VideoDownloadService : IVideoDownloadService
{
    private readonly YoutubeClient _youtubeClient;
    private readonly ITempFileManagementService _tempFileService;
    private readonly ILogger<VideoDownloadService> _logger;

    public VideoDownloadService(
        ITempFileManagementService tempFileService,
        ILogger<VideoDownloadService> logger)
    {
        _tempFileService = tempFileService;
        _logger = logger;
        _youtubeClient = new YoutubeClient();
    }

    /// <inheritdoc/>
    public async Task<string> DownloadVideoAsync(
        string youTubeId,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting video download for YouTube ID: {YouTubeId}", youTubeId);

            // Step 1: Get stream manifest
            _logger.LogDebug("Fetching stream manifest for: {YouTubeId}", youTubeId);
            var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(youTubeId, cancellationToken);

            // Step 2: Select BEST muxed stream (video + audio in one file, MP4 preferred)
            var streamInfo = streamManifest
                .GetMuxedStreams()
                .Where(s => s.Container == Container.Mp4)
                .OrderByDescending(s => s.VideoQuality.MaxHeight)
                .ThenByDescending(s => s.Bitrate)
                .FirstOrDefault();

            // Fallback to any muxed stream if MP4 not available
            if (streamInfo == null)
            {
                _logger.LogWarning("No MP4 muxed stream found, trying any muxed stream for: {YouTubeId}", youTubeId);
                streamInfo = streamManifest
                    .GetMuxedStreams()
                    .OrderByDescending(s => s.VideoQuality.MaxHeight)
                    .ThenByDescending(s => s.Bitrate)
                    .FirstOrDefault();
            }

            if (streamInfo == null)
            {
                throw new InvalidOperationException(
                    $"No suitable video stream found for YouTube ID: {youTubeId}. " +
                    "The video may be unavailable, private, or restricted.");
            }

            _logger.LogInformation(
                "Selected video stream for {YouTubeId}: Container={Container}, Quality={Quality}, " +
                "Bitrate={Bitrate}, Size={SizeMB}MB",
                youTubeId, streamInfo.Container.Name, streamInfo.VideoQuality.Label,
                streamInfo.Bitrate, streamInfo.Size.MegaBytes);

            // Step 3: Check disk space (require 2x video size for buffer)
            var requiredSpace = streamInfo.Size.Bytes * 2;
            var hasSufficientSpace = await _tempFileService.HasSufficientDiskSpaceAsync(
                streamInfo.Size.Bytes, cancellationToken);

            if (!hasSufficientSpace)
            {
                var availableSpace = await _tempFileService.GetAvailableDiskSpaceAsync();
                var requiredMB = requiredSpace / (1024.0 * 1024.0);
                var availableMB = availableSpace / (1024.0 * 1024.0);

                throw new InvalidOperationException(
                    $"Insufficient disk space for video download. " +
                    $"Required: {requiredMB:F2}MB (with 2x buffer), Available: {availableMB:F2}MB");
            }

            // Step 4: Generate output file path
            var extension = streamInfo.Container.Name.ToLowerInvariant();
            if (!extension.StartsWith("."))
            {
                extension = "." + extension;
            }

            var outputPath = _tempFileService.GenerateFilePath(youTubeId, extension);

            _logger.LogDebug("Video will be downloaded to: {OutputPath}", outputPath);

            // Step 5: Download with progress tracking
            var progressHandler = new Progress<double>(p =>
            {
                progress?.Report(p);

                // Log every 10% progress
                if (p % 0.1 < 0.01 || p >= 0.99)
                {
                    _logger.LogInformation("Download progress for {YouTubeId}: {Progress:P0}", youTubeId, p);
                }
            });

            await _youtubeClient.Videos.Streams.DownloadAsync(
                streamInfo,
                outputPath,
                progressHandler,
                cancellationToken);

            var downloadDuration = (DateTime.UtcNow - startTime).TotalSeconds;

            // Step 6: Verify downloaded file
            var fileInfo = new FileInfo(outputPath);
            if (!fileInfo.Exists)
            {
                throw new InvalidOperationException(
                    $"Download completed but file not found: {outputPath}");
            }

            if (fileInfo.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Downloaded file is empty: {outputPath}");
            }

            _logger.LogInformation(
                "Video downloaded successfully for {YouTubeId} in {DurationSeconds:F2}s. " +
                "Path: {Path}, Size: {SizeMB:F2}MB",
                youTubeId, downloadDuration, outputPath, fileInfo.Length / (1024.0 * 1024.0));

            return outputPath;
        }
        catch (Exception ex)
        {
            var failureDuration = (DateTime.UtcNow - startTime).TotalSeconds;
            _logger.LogError(ex,
                "Video download failed for {YouTubeId} after {DurationSeconds:F2}s. Error: {ErrorMessage}",
                youTubeId, failureDuration, ex.Message);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> DownloadVideoWithDetailsAsync(
        string youTubeId,
        IProgress<VideoDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var lastReportTime = startTime;
        long totalBytes = 0;
        long downloadedBytes = 0;

        try
        {
            _logger.LogInformation("Starting detailed video download for: {YouTubeId}", youTubeId);

            // Get stream info first to know total size
            var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(youTubeId, cancellationToken);
            var streamInfo = streamManifest
                .GetMuxedStreams()
                .Where(s => s.Container == Container.Mp4)
                .OrderByDescending(s => s.VideoQuality.MaxHeight)
                .FirstOrDefault() ?? streamManifest.GetMuxedStreams().First();

            totalBytes = streamInfo.Size.Bytes;

            // Create detailed progress handler
            var simpleProgress = new Progress<double>(percentage =>
            {
                downloadedBytes = (long)(totalBytes * percentage);
                var elapsed = DateTime.UtcNow - startTime;
                var bytesPerSecond = elapsed.TotalSeconds > 0
                    ? downloadedBytes / elapsed.TotalSeconds
                    : 0;
                var remainingBytes = totalBytes - downloadedBytes;
                var eta = bytesPerSecond > 0
                    ? TimeSpan.FromSeconds(remainingBytes / bytesPerSecond)
                    : (TimeSpan?)null;

                // Report every 10 seconds to avoid spam
                if ((DateTime.UtcNow - lastReportTime).TotalSeconds >= 10 || percentage >= 0.99)
                {
                    progress?.Report(new VideoDownloadProgress
                    {
                        Percentage = percentage * 100,
                        BytesDownloaded = downloadedBytes,
                        TotalBytes = totalBytes,
                        BytesPerSecond = bytesPerSecond,
                        EstimatedTimeRemaining = eta
                    });

                    _logger.LogInformation(
                        "Download progress for {YouTubeId}: {Progress:P0}, Speed: {Speed}, ETA: {ETA}",
                        youTubeId, percentage,
                        FormatBytesPerSecond(bytesPerSecond),
                        eta?.ToString(@"hh\:mm\:ss") ?? "calculating...");

                    lastReportTime = DateTime.UtcNow;
                }
            });

            // Use the simple download method with detailed progress wrapper
            return await DownloadVideoAsync(youTubeId, simpleProgress, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Detailed video download failed for: {YouTubeId}", youTubeId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<AudioStreamInfo> GetBestAudioStreamAsync(
        string youTubeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting best audio stream for: {YouTubeId}", youTubeId);

            var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(youTubeId, cancellationToken);

            var audioStream = streamManifest
                .GetAudioOnlyStreams()
                .OrderByDescending(s => s.Bitrate)
                .FirstOrDefault();

            if (audioStream == null)
            {
                throw new InvalidOperationException(
                    $"No audio stream found for YouTube ID: {youTubeId}");
            }

            var audioStreamInfo = new AudioStreamInfo
            {
                Container = audioStream.Container.Name,
                Bitrate = audioStream.Bitrate.BitsPerSecond,
                Size = audioStream.Size.Bytes,
                Codec = audioStream.AudioCodec
            };

            _logger.LogDebug(
                "Best audio stream for {YouTubeId}: Bitrate={Bitrate}, Size={Size}, Codec={Codec}",
                youTubeId, audioStreamInfo.Bitrate, audioStreamInfo.FormattedSize, audioStreamInfo.Codec);

            return audioStreamInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audio stream info for: {YouTubeId}", youTubeId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsVideoAvailableAsync(
        string youTubeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking video availability for: {YouTubeId}", youTubeId);

            // Try to get video metadata
            var video = await _youtubeClient.Videos.GetAsync(youTubeId, cancellationToken);

            if (video == null)
            {
                _logger.LogWarning("Video not found: {YouTubeId}", youTubeId);
                return false;
            }

            // Try to get stream manifest
            var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(youTubeId, cancellationToken);

            if (streamManifest == null || !streamManifest.Streams.Any())
            {
                _logger.LogWarning("No streams available for video: {YouTubeId}", youTubeId);
                return false;
            }

            _logger.LogDebug("Video is available: {YouTubeId}, Title: {Title}", youTubeId, video.Title);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Video not available: {YouTubeId}. Error: {ErrorMessage}",
                youTubeId, ex.Message);
            return false;
        }
    }

    private static string FormatBytesPerSecond(double bytesPerSecond)
    {
        string[] sizes = { "B/s", "KB/s", "MB/s", "GB/s" };
        int order = 0;
        double size = bytesPerSecond;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}
