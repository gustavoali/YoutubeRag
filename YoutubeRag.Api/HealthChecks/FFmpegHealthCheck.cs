using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace YoutubeRag.Api.HealthChecks;

/// <summary>
/// Health check to verify FFmpeg is installed and accessible
/// </summary>
public class FFmpegHealthCheck : IHealthCheck
{
    private readonly ILogger<FFmpegHealthCheck> _logger;

    public FFmpegHealthCheck(ILogger<FFmpegHealthCheck> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Checks if FFmpeg is installed and can be executed
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Create process to execute FFmpeg version command
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };

            process.Start();

            // Read output asynchronously
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                // Extract version from output (first line typically contains version)
                var versionLine = output.Split('\n').FirstOrDefault()?.Trim() ?? "Unknown";

                _logger.LogDebug("FFmpeg health check passed. Version: {Version}", versionLine);

                return HealthCheckResult.Healthy(
                    description: "FFmpeg is accessible",
                    data: new Dictionary<string, object>
                    {
                        { "version", versionLine },
                        { "executable", "ffmpeg" }
                    });
            }
            else
            {
                _logger.LogWarning(
                    "FFmpeg health check failed. Exit code: {ExitCode}, Error: {Error}",
                    process.ExitCode,
                    error);

                return HealthCheckResult.Unhealthy(
                    description: $"FFmpeg execution failed with exit code {process.ExitCode}",
                    data: new Dictionary<string, object>
                    {
                        { "exit_code", process.ExitCode },
                        { "error", error }
                    });
            }
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.Message.Contains("cannot find"))
        {
            _logger.LogWarning("FFmpeg health check failed: FFmpeg not found in PATH");

            return HealthCheckResult.Unhealthy(
                description: "FFmpeg is not installed or not in PATH",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    { "error", "FFmpeg executable not found" },
                    { "suggestion", "Install FFmpeg and ensure it's in the system PATH" }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FFmpeg health check failed with unexpected error");

            return HealthCheckResult.Unhealthy(
                description: "FFmpeg health check failed",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    { "error", ex.Message }
                });
        }
    }
}
