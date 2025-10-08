namespace YoutubeRag.Application.Interfaces;

/// <summary>
/// Service interface for managing temporary files in the video processing pipeline
/// </summary>
public interface ITempFileManagementService
{
    /// <summary>
    /// Creates a unique directory for a video
    /// </summary>
    /// <param name="videoId">Video ID</param>
    /// <returns>Path to the created directory</returns>
    string CreateVideoDirectory(string videoId);

    /// <summary>
    /// Generates a unique file path for a video
    /// </summary>
    /// <param name="videoId">Video ID</param>
    /// <param name="extension">File extension (e.g., .mp4, .wav)</param>
    /// <returns>Unique file path with timestamp</returns>
    string GenerateFilePath(string videoId, string extension);

    /// <summary>
    /// Checks if there is sufficient disk space for a download
    /// </summary>
    /// <param name="requiredSizeBytes">Required size in bytes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if sufficient space is available</returns>
    Task<bool> HasSufficientDiskSpaceAsync(long requiredSizeBytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available disk space in bytes
    /// </summary>
    /// <returns>Available space in bytes</returns>
    Task<long> GetAvailableDiskSpaceAsync();

    /// <summary>
    /// Cleans up files older than specified hours
    /// </summary>
    /// <param name="olderThanHours">Delete files older than this many hours</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of files deleted</returns>
    Task<int> CleanupOldFilesAsync(int olderThanHours, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a specific file
    /// </summary>
    /// <param name="filePath">Path to the file to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deletion was successful</returns>
    Task<bool> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all files in a video directory
    /// </summary>
    /// <param name="videoId">Video ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of files deleted</returns>
    Task<int> DeleteVideoFilesAsync(string videoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total size of files for a video
    /// </summary>
    /// <param name="videoId">Video ID</param>
    /// <returns>Total size in bytes</returns>
    Task<long> GetVideoFilesSizeAsync(string videoId);

    /// <summary>
    /// Gets statistics about temporary file storage
    /// </summary>
    /// <returns>Storage statistics</returns>
    Task<TempFileStorageStats> GetStorageStatsAsync();

    /// <summary>
    /// Gets the base temporary file path
    /// </summary>
    /// <returns>Base path for temporary files</returns>
    string GetBaseTempPath();
}

/// <summary>
/// Statistics about temporary file storage
/// </summary>
public class TempFileStorageStats
{
    /// <summary>
    /// Total number of files
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Total size of all files in bytes
    /// </summary>
    public long TotalSizeBytes { get; set; }

    /// <summary>
    /// Number of video directories
    /// </summary>
    public int VideoDirectoryCount { get; set; }

    /// <summary>
    /// Available disk space in bytes
    /// </summary>
    public long AvailableDiskSpaceBytes { get; set; }

    /// <summary>
    /// Human-readable total size
    /// </summary>
    public string FormattedTotalSize => FormatBytes(TotalSizeBytes);

    /// <summary>
    /// Human-readable available space
    /// </summary>
    public string FormattedAvailableSpace => FormatBytes(AvailableDiskSpaceBytes);

    /// <summary>
    /// Oldest file age
    /// </summary>
    public TimeSpan? OldestFileAge { get; set; }

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
