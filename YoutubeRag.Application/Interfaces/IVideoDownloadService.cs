namespace YoutubeRag.Application.Interfaces;

/// <summary>
/// Service interface for downloading YouTube videos
/// </summary>
public interface IVideoDownloadService
{
    /// <summary>
    /// Downloads a YouTube video with progress tracking
    /// </summary>
    /// <param name="youTubeId">YouTube video ID</param>
    /// <param name="progress">Progress reporter for download tracking</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Path to the downloaded video file</returns>
    Task<string> DownloadVideoAsync(
        string youTubeId,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a YouTube video with detailed progress information
    /// </summary>
    /// <param name="youTubeId">YouTube video ID</param>
    /// <param name="progress">Progress reporter with detailed download information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Path to the downloaded video file</returns>
    Task<string> DownloadVideoWithDetailsAsync(
        string youTubeId,
        IProgress<VideoDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the best available audio stream information for a video
    /// </summary>
    /// <param name="youTubeId">YouTube video ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Audio stream information</returns>
    Task<AudioStreamInfo> GetBestAudioStreamAsync(
        string youTubeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a video is available for download
    /// </summary>
    /// <param name="youTubeId">YouTube video ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if video is available for download</returns>
    Task<bool> IsVideoAvailableAsync(
        string youTubeId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Detailed progress information for video download
/// </summary>
public class VideoDownloadProgress
{
    /// <summary>
    /// Percentage of download completed (0-100)
    /// </summary>
    public double Percentage { get; set; }

    /// <summary>
    /// Bytes downloaded so far
    /// </summary>
    public long BytesDownloaded { get; set; }

    /// <summary>
    /// Total bytes to download
    /// </summary>
    public long TotalBytes { get; set; }

    /// <summary>
    /// Estimated time remaining
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; set; }

    /// <summary>
    /// Download speed in bytes per second
    /// </summary>
    public double BytesPerSecond { get; set; }

    /// <summary>
    /// Human-readable download speed
    /// </summary>
    public string FormattedSpeed => FormatSpeed(BytesPerSecond);

    /// <summary>
    /// Human-readable progress
    /// </summary>
    public string FormattedProgress => $"{FormatBytes(BytesDownloaded)} / {FormatBytes(TotalBytes)}";

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }

    private static string FormatSpeed(double bytesPerSecond)
    {
        return $"{FormatBytes((long)bytesPerSecond)}/s";
    }
}

/// <summary>
/// Information about an audio stream
/// </summary>
public class AudioStreamInfo
{
    /// <summary>
    /// Stream container format (e.g., mp4, webm)
    /// </summary>
    public string Container { get; set; } = string.Empty;

    /// <summary>
    /// Bitrate in bits per second
    /// </summary>
    public long Bitrate { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Human-readable size
    /// </summary>
    public string FormattedSize => FormatBytes(Size);

    /// <summary>
    /// Audio codec
    /// </summary>
    public string Codec { get; set; } = string.Empty;

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}
