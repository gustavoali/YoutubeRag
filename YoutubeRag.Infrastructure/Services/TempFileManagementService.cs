using Microsoft.Extensions.Logging;
using YoutubeRag.Application.Configuration;
using YoutubeRag.Application.Interfaces;

namespace YoutubeRag.Infrastructure.Services;

/// <summary>
/// Service for managing temporary files in the video processing pipeline
/// </summary>
public class TempFileManagementService : ITempFileManagementService
{
    private readonly ILogger<TempFileManagementService> _logger;
    private readonly IAppConfiguration _appConfiguration;
    private readonly string _baseTempPath;

    public TempFileManagementService(
        ILogger<TempFileManagementService> logger,
        IAppConfiguration appConfiguration)
    {
        _logger = logger;
        _appConfiguration = appConfiguration;

        // Use configured temp path or default to system temp + YoutubeRag
        _baseTempPath = _appConfiguration.TempFilePath
            ?? Path.Combine(Path.GetTempPath(), "YoutubeRag");

        // Ensure base directory exists
        if (!Directory.Exists(_baseTempPath))
        {
            Directory.CreateDirectory(_baseTempPath);
            _logger.LogInformation("Created base temp directory: {Path}", _baseTempPath);
        }
    }

    /// <inheritdoc/>
    public string CreateVideoDirectory(string videoId)
    {
        if (string.IsNullOrWhiteSpace(videoId))
        {
            throw new ArgumentException("Video ID cannot be null or empty", nameof(videoId));
        }

        try
        {
            var videoDir = Path.Combine(_baseTempPath, videoId);

            if (!Directory.Exists(videoDir))
            {
                Directory.CreateDirectory(videoDir);
                _logger.LogInformation("Created video directory: {Path}", videoDir);
            }

            return videoDir;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create video directory for: {VideoId}", videoId);
            throw;
        }
    }

    /// <inheritdoc/>
    public string GenerateFilePath(string videoId, string extension)
    {
        if (string.IsNullOrWhiteSpace(videoId))
        {
            throw new ArgumentException("Video ID cannot be null or empty", nameof(videoId));
        }

        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new ArgumentException("Extension cannot be null or empty", nameof(extension));
        }

        try
        {
            // Ensure extension starts with dot
            if (!extension.StartsWith("."))
            {
                extension = "." + extension;
            }

            // Create video directory
            var videoDir = CreateVideoDirectory(videoId);

            // Generate unique filename with timestamp
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var fileName = $"{videoId}_{timestamp}{extension}";
            var filePath = Path.Combine(videoDir, fileName);

            _logger.LogDebug("Generated file path: {FilePath}", filePath);

            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate file path for video: {VideoId}, extension: {Extension}",
                videoId, extension);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> HasSufficientDiskSpaceAsync(long requiredSizeBytes, CancellationToken cancellationToken = default)
    {
        try
        {
            var availableSpace = await GetAvailableDiskSpaceAsync();
            var requiredWithBuffer = requiredSizeBytes * 2; // 2x buffer for safety

            _logger.LogDebug(
                "Disk space check: Required={RequiredMB}MB (with buffer={BufferMB}MB), Available={AvailableMB}MB",
                requiredSizeBytes / (1024.0 * 1024.0),
                requiredWithBuffer / (1024.0 * 1024.0),
                availableSpace / (1024.0 * 1024.0));

            return availableSpace >= requiredWithBuffer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check disk space");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<long> GetAvailableDiskSpaceAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var rootPath = Path.GetPathRoot(_baseTempPath);
                if (string.IsNullOrEmpty(rootPath))
                {
                    _logger.LogWarning("Could not determine root path from: {BasePath}", _baseTempPath);
                    return 0;
                }

                var drive = new DriveInfo(rootPath);

                if (!drive.IsReady)
                {
                    _logger.LogWarning("Drive not ready: {DriveName}", drive.Name);
                    return 0;
                }

                _logger.LogDebug("Drive {DriveName} available space: {AvailableGB}GB",
                    drive.Name, drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0));

                return drive.AvailableFreeSpace;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get available disk space");
                return 0;
            }
        });
    }

    /// <inheritdoc/>
    public async Task<int> CleanupOldFilesAsync(int olderThanHours, CancellationToken cancellationToken = default)
    {
        var deletedCount = 0;

        try
        {
            if (!Directory.Exists(_baseTempPath))
            {
                _logger.LogWarning("Base temp path does not exist: {Path}", _baseTempPath);
                return 0;
            }

            var cutoffDate = DateTime.UtcNow.AddHours(-olderThanHours);
            _logger.LogInformation("Starting cleanup of files older than {CutoffDate} (>{Hours} hours)",
                cutoffDate, olderThanHours);

            // Get all video directories
            var videoDirectories = Directory.GetDirectories(_baseTempPath);

            foreach (var videoDir in videoDirectories)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Cleanup cancelled by user");
                    break;
                }

                try
                {
                    // Check all files in the directory
                    var files = Directory.GetFiles(videoDir);
                    var oldFiles = files.Where(f =>
                    {
                        var fileInfo = new FileInfo(f);
                        return fileInfo.LastWriteTimeUtc < cutoffDate;
                    }).ToList();

                    // Delete old files
                    foreach (var file in oldFiles)
                    {
                        try
                        {
                            File.Delete(file);
                            deletedCount++;
                            _logger.LogDebug("Deleted old file: {FilePath}", file);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete file: {FilePath}", file);
                        }
                    }

                    // Delete directory if it's empty
                    if (!Directory.GetFiles(videoDir).Any())
                    {
                        try
                        {
                            Directory.Delete(videoDir);
                            _logger.LogDebug("Deleted empty directory: {DirPath}", videoDir);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete directory: {DirPath}", videoDir);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process directory: {DirPath}", videoDir);
                }
            }

            _logger.LogInformation("Cleanup completed. Deleted {Count} old files", deletedCount);

            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup of old files");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _logger.LogWarning("Cannot delete file: path is null or empty");
                return false;
            }

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File not found for deletion: {FilePath}", filePath);
                return false;
            }

            await Task.Run(() => File.Delete(filePath), cancellationToken);
            _logger.LogInformation("Deleted file: {FilePath}", filePath);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {FilePath}", filePath);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<int> DeleteVideoFilesAsync(string videoId, CancellationToken cancellationToken = default)
    {
        var deletedCount = 0;

        try
        {
            if (string.IsNullOrWhiteSpace(videoId))
            {
                _logger.LogWarning("Cannot delete video files: videoId is null or empty");
                return 0;
            }

            var videoDir = Path.Combine(_baseTempPath, videoId);

            if (!Directory.Exists(videoDir))
            {
                _logger.LogWarning("Video directory not found: {VideoDir}", videoDir);
                return 0;
            }

            var files = Directory.GetFiles(videoDir);

            foreach (var file in files)
            {
                try
                {
                    await Task.Run(() => File.Delete(file), cancellationToken);
                    deletedCount++;
                    _logger.LogDebug("Deleted video file: {FilePath}", file);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete video file: {FilePath}", file);
                }
            }

            // Delete the directory itself if empty
            if (!Directory.GetFiles(videoDir).Any())
            {
                try
                {
                    Directory.Delete(videoDir);
                    _logger.LogInformation("Deleted video directory: {VideoDir}", videoDir);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete video directory: {VideoDir}", videoDir);
                }
            }

            _logger.LogInformation("Deleted {Count} files for video: {VideoId}", deletedCount, videoId);

            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting video files for: {VideoId}", videoId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<long> GetVideoFilesSizeAsync(string videoId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(videoId))
            {
                _logger.LogWarning("Cannot get video files size: videoId is null or empty");
                return 0;
            }

            var videoDir = Path.Combine(_baseTempPath, videoId);

            if (!Directory.Exists(videoDir))
            {
                _logger.LogDebug("Video directory not found: {VideoDir}", videoDir);
                return 0;
            }

            return await Task.Run(() =>
            {
                var files = Directory.GetFiles(videoDir);
                long totalSize = 0;

                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        totalSize += fileInfo.Length;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get size of file: {FilePath}", file);
                    }
                }

                _logger.LogDebug("Total size for video {VideoId}: {SizeMB}MB",
                    videoId, totalSize / (1024.0 * 1024.0));

                return totalSize;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get video files size for: {VideoId}", videoId);
            return 0;
        }
    }

    /// <inheritdoc/>
    public async Task<TempFileStorageStats> GetStorageStatsAsync()
    {
        try
        {
            return await Task.Run(() =>
            {
                var stats = new TempFileStorageStats();

                if (!Directory.Exists(_baseTempPath))
                {
                    _logger.LogWarning("Base temp path does not exist: {Path}", _baseTempPath);
                    return stats;
                }

                var videoDirectories = Directory.GetDirectories(_baseTempPath);
                stats.VideoDirectoryCount = videoDirectories.Length;

                long totalSize = 0;
                int totalFiles = 0;
                DateTime? oldestFileDate = null;

                foreach (var videoDir in videoDirectories)
                {
                    try
                    {
                        var files = Directory.GetFiles(videoDir);
                        totalFiles += files.Length;

                        foreach (var file in files)
                        {
                            try
                            {
                                var fileInfo = new FileInfo(file);
                                totalSize += fileInfo.Length;

                                if (!oldestFileDate.HasValue || fileInfo.LastWriteTimeUtc < oldestFileDate.Value)
                                {
                                    oldestFileDate = fileInfo.LastWriteTimeUtc;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to get info for file: {FilePath}", file);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process directory: {DirPath}", videoDir);
                    }
                }

                stats.TotalFiles = totalFiles;
                stats.TotalSizeBytes = totalSize;
                stats.AvailableDiskSpaceBytes = GetAvailableDiskSpaceAsync().GetAwaiter().GetResult();

                if (oldestFileDate.HasValue)
                {
                    stats.OldestFileAge = DateTime.UtcNow - oldestFileDate.Value;
                }

                _logger.LogDebug(
                    "Storage stats: {TotalFiles} files, {TotalSize}, {VideoDirectories} directories, {AvailableSpace} available",
                    stats.TotalFiles, stats.FormattedTotalSize, stats.VideoDirectoryCount, stats.FormattedAvailableSpace);

                return stats;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get storage stats");
            return new TempFileStorageStats();
        }
    }

    /// <inheritdoc/>
    public string GetBaseTempPath()
    {
        return _baseTempPath;
    }
}
