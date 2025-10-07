using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using YoutubeRag.Api.Configuration;

namespace YoutubeRag.Api.HealthChecks;

/// <summary>
/// Health check to verify sufficient disk space is available
/// </summary>
public class DiskSpaceHealthCheck : IHealthCheck
{
    private readonly ILogger<DiskSpaceHealthCheck> _logger;
    private readonly AppSettings _appSettings;

    // Thresholds in GB
    private const long MinimumHealthyDiskSpaceGB = 10;
    private const long MinimumDegradedDiskSpaceGB = 5;

    public DiskSpaceHealthCheck(
        ILogger<DiskSpaceHealthCheck> logger,
        IOptions<AppSettings> appSettings)
    {
        _logger = logger;
        _appSettings = appSettings.Value;
    }

    /// <summary>
    /// Checks available disk space on the data directory drive
    /// </summary>
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get the data directory path from configuration
            var dataDirectory = _appSettings.AudioStoragePath ?? "./data/audio";
            var fullPath = Path.GetFullPath(dataDirectory);

            // Get the drive information
            var driveInfo = GetDriveInfo(fullPath);

            if (driveInfo == null)
            {
                _logger.LogWarning("Unable to determine drive for path: {Path}", fullPath);

                return Task.FromResult(HealthCheckResult.Degraded(
                    description: "Unable to determine drive information",
                    data: new Dictionary<string, object>
                    {
                        { "data_directory", fullPath },
                        { "warning", "Drive information not available" }
                    }));
            }

            // Calculate available space in GB
            var availableSpaceGB = driveInfo.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
            var totalSpaceGB = driveInfo.TotalSize / (1024.0 * 1024.0 * 1024.0);
            var usedSpaceGB = totalSpaceGB - availableSpaceGB;
            var usedPercentage = (usedSpaceGB / totalSpaceGB) * 100;

            var data = new Dictionary<string, object>
            {
                { "drive", driveInfo.Name },
                { "data_directory", fullPath },
                { "available_space_gb", Math.Round(availableSpaceGB, 2) },
                { "total_space_gb", Math.Round(totalSpaceGB, 2) },
                { "used_space_gb", Math.Round(usedSpaceGB, 2) },
                { "used_percentage", Math.Round(usedPercentage, 2) },
                { "drive_format", driveInfo.DriveFormat },
                { "drive_type", driveInfo.DriveType.ToString() }
            };

            // Determine health status based on available space
            if (availableSpaceGB >= MinimumHealthyDiskSpaceGB)
            {
                _logger.LogDebug(
                    "Disk space health check passed. Available: {AvailableGB:F2}GB on {Drive}",
                    availableSpaceGB,
                    driveInfo.Name);

                return Task.FromResult(HealthCheckResult.Healthy(
                    description: $"Sufficient disk space available ({availableSpaceGB:F2}GB)",
                    data: data));
            }
            else if (availableSpaceGB >= MinimumDegradedDiskSpaceGB)
            {
                _logger.LogWarning(
                    "Disk space is running low. Available: {AvailableGB:F2}GB on {Drive} (threshold: {ThresholdGB}GB)",
                    availableSpaceGB,
                    driveInfo.Name,
                    MinimumHealthyDiskSpaceGB);

                data["warning"] = $"Disk space below recommended threshold of {MinimumHealthyDiskSpaceGB}GB";

                return Task.FromResult(HealthCheckResult.Degraded(
                    description: $"Low disk space warning ({availableSpaceGB:F2}GB available)",
                    data: data));
            }
            else
            {
                _logger.LogError(
                    "Critical: Disk space critically low. Available: {AvailableGB:F2}GB on {Drive} (minimum: {MinimumGB}GB)",
                    availableSpaceGB,
                    driveInfo.Name,
                    MinimumDegradedDiskSpaceGB);

                data["error"] = $"Critical disk space shortage. Less than {MinimumDegradedDiskSpaceGB}GB available";
                data["action_required"] = "Free up disk space immediately or processing may fail";

                return Task.FromResult(HealthCheckResult.Unhealthy(
                    description: $"Critical disk space shortage ({availableSpaceGB:F2}GB available)",
                    data: data));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Disk space health check failed with unexpected error");

            return Task.FromResult(HealthCheckResult.Unhealthy(
                description: "Disk space health check failed",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    { "error", ex.Message }
                }));
        }
    }

    /// <summary>
    /// Gets the DriveInfo for a given path
    /// </summary>
    private DriveInfo? GetDriveInfo(string path)
    {
        try
        {
            // Ensure the directory exists or get its parent
            var directory = Directory.Exists(path) ? path : Path.GetDirectoryName(path);

            if (string.IsNullOrEmpty(directory))
            {
                directory = Directory.GetCurrentDirectory();
            }

            // Get the root path for the directory
            var rootPath = Path.GetPathRoot(Path.GetFullPath(directory));

            if (string.IsNullOrEmpty(rootPath))
            {
                return null;
            }

            // Find the matching drive
            return DriveInfo.GetDrives()
                .FirstOrDefault(d => d.IsReady && d.Name.Equals(rootPath, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get drive information for path: {Path}", path);
            return null;
        }
    }
}
