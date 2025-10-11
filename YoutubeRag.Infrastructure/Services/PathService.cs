using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using YoutubeRag.Application.Interfaces;

namespace YoutubeRag.Infrastructure.Services
{
    /// <summary>
    /// Cross-platform path resolution service for file system operations.
    /// Implements IPathProvider to provide consistent path handling across all environments.
    /// </summary>
    /// <remarks>
    /// DEVOPS-002: Cross-Platform PathService Implementation
    ///
    /// This service solves the critical problem of path inconsistencies between:
    /// - Windows development (C:\Temp\YoutubeRag)
    /// - Linux/Mac development (/tmp/youtuberag)
    /// - Docker containers (/app/temp)
    /// - CI/CD environments (/tmp/youtuberag)
    ///
    /// Configuration Priority (highest to lowest):
    /// 1. Environment Variables (PROCESSING_TEMP_PATH, WHISPER_MODELS_PATH, etc.)
    /// 2. appsettings.json configuration
    /// 3. Platform-specific defaults
    ///
    /// Container Detection:
    /// - Checks for /.dockerenv file (Docker)
    /// - Checks DOTNET_RUNNING_IN_CONTAINER environment variable
    /// - Uses /app/* paths when in container
    /// </remarks>
    public class PathService : IPathProvider
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PathService> _logger;
        private readonly bool _isRunningInContainer;
        private readonly bool _isWindows;

        // Lazy-initialized cached paths
        private string? _tempPath;
        private string? _modelsPath;
        private string? _uploadsPath;
        private string? _logsPath;

        public PathService(IConfiguration configuration, ILogger<PathService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            _isRunningInContainer = DetectContainerEnvironment();

            _logger.LogInformation(
                "PathService initialized - Platform: {Platform}, Container: {IsContainer}",
                _isWindows ? "Windows" : "Unix",
                _isRunningInContainer);
        }

        /// <inheritdoc />
        public string GetTempPath()
        {
            if (_tempPath != null)
            {
                return _tempPath;
            }

            _tempPath = ResolvePath(
                environmentVariableName: "PROCESSING_TEMP_PATH",
                configurationKey: "Processing:TempFilePath",
                defaultPath: GetDefaultTempPath());

            EnsureDirectoryExists(_tempPath);

            _logger.LogDebug("Temp path resolved: {TempPath}", _tempPath);
            return _tempPath;
        }

        /// <inheritdoc />
        public string GetModelsPath()
        {
            if (_modelsPath != null)
            {
                return _modelsPath;
            }

            _modelsPath = ResolvePath(
                environmentVariableName: "WHISPER_MODELS_PATH",
                configurationKey: "Whisper:ModelsPath",
                defaultPath: GetDefaultModelsPath());

            EnsureDirectoryExists(_modelsPath);

            _logger.LogDebug("Models path resolved: {ModelsPath}", _modelsPath);
            return _modelsPath;
        }

        /// <inheritdoc />
        public string GetUploadsPath()
        {
            if (_uploadsPath != null)
            {
                return _uploadsPath;
            }

            _uploadsPath = ResolvePath(
                environmentVariableName: "UPLOAD_PATH",
                configurationKey: "Processing:UploadPath",
                defaultPath: GetDefaultUploadsPath());

            EnsureDirectoryExists(_uploadsPath);

            _logger.LogDebug("Uploads path resolved: {UploadsPath}", _uploadsPath);
            return _uploadsPath;
        }

        /// <inheritdoc />
        public string GetLogsPath()
        {
            if (_logsPath != null)
            {
                return _logsPath;
            }

            // Logs are always relative to application directory
            _logsPath = Path.Combine(AppContext.BaseDirectory, "logs");
            EnsureDirectoryExists(_logsPath);

            _logger.LogDebug("Logs path resolved: {LogsPath}", _logsPath);
            return _logsPath;
        }

        /// <inheritdoc />
        public string CombinePath(params string[] paths)
        {
            if (paths == null || paths.Length == 0)
            {
                throw new ArgumentException("At least one path must be provided", nameof(paths));
            }

            var combined = Path.Combine(paths);
            return NormalizePath(combined);
        }

        /// <inheritdoc />
        public string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or whitespace", nameof(path));
            }

            // Replace wrong separators with correct ones
            var normalized = path.Replace('\\', Path.DirectorySeparatorChar)
                                 .Replace('/', Path.DirectorySeparatorChar);

            // Remove duplicate separators
            while (normalized.Contains($"{Path.DirectorySeparatorChar}{Path.DirectorySeparatorChar}"))
            {
                normalized = normalized.Replace(
                    $"{Path.DirectorySeparatorChar}{Path.DirectorySeparatorChar}",
                    Path.DirectorySeparatorChar.ToString());
            }

            // Get full path to resolve relative paths and .. references
            try
            {
                normalized = Path.GetFullPath(normalized);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get full path for {Path}, using as-is", path);
            }

            return normalized;
        }

        /// <inheritdoc />
        public string EnsureDirectoryExists(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or whitespace", nameof(path));
            }

            var normalizedPath = NormalizePath(path);

            if (!Directory.Exists(normalizedPath))
            {
                try
                {
                    Directory.CreateDirectory(normalizedPath);
                    _logger.LogInformation("Created directory: {Path}", normalizedPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create directory: {Path}", normalizedPath);
                    throw;
                }
            }

            return normalizedPath;
        }

        /// <inheritdoc />
        public string GetTempFilePath(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                throw new ArgumentException("Extension cannot be null or whitespace", nameof(extension));
            }

            // Ensure extension has leading dot
            if (!extension.StartsWith("."))
            {
                extension = "." + extension;
            }

            var tempDir = GetTempPath();
            var fileName = $"{Guid.NewGuid():N}{extension}";
            return CombinePath(tempDir, fileName);
        }

        /// <inheritdoc />
        public bool IsRunningInContainer()
        {
            return _isRunningInContainer;
        }

        /// <inheritdoc />
        public char GetPathSeparator()
        {
            return Path.DirectorySeparatorChar;
        }

        /// <inheritdoc />
        public string ResolvePath(string environmentVariableName, string configurationKey, string defaultPath)
        {
            // Priority 1: Environment Variable
            var envValue = Environment.GetEnvironmentVariable(environmentVariableName);
            if (!string.IsNullOrWhiteSpace(envValue))
            {
                _logger.LogDebug(
                    "Using environment variable {EnvVar} = {Value}",
                    environmentVariableName,
                    envValue);
                return NormalizePath(envValue);
            }

            // Priority 2: Configuration (appsettings.json)
            var configValue = _configuration[configurationKey];
            if (!string.IsNullOrWhiteSpace(configValue))
            {
                _logger.LogDebug(
                    "Using configuration {ConfigKey} = {Value}",
                    configurationKey,
                    configValue);
                return NormalizePath(configValue);
            }

            // Priority 3: Default path
            _logger.LogDebug(
                "Using default path for {EnvVar}: {DefaultPath}",
                environmentVariableName,
                defaultPath);
            return NormalizePath(defaultPath);
        }

        // ============================================
        // Private Helper Methods
        // ============================================

        /// <summary>
        /// Detects if the application is running in a Docker container.
        /// </summary>
        private bool DetectContainerEnvironment()
        {
            // Method 1: Check for .dockerenv file (standard Docker indicator)
            if (File.Exists("/.dockerenv"))
            {
                _logger.LogInformation("Container detected via /.dockerenv file");
                return true;
            }

            // Method 2: Check environment variable set by .NET in containers
            var dotnetInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
            if (!string.IsNullOrEmpty(dotnetInContainer) && dotnetInContainer == "true")
            {
                _logger.LogInformation("Container detected via DOTNET_RUNNING_IN_CONTAINER env var");
                return true;
            }

            // Method 3: Check for container-specific cgroup
            if (!_isWindows && File.Exists("/proc/1/cgroup"))
            {
                try
                {
                    var cgroup = File.ReadAllText("/proc/1/cgroup");
                    if (cgroup.Contains("docker") || cgroup.Contains("kubepods"))
                    {
                        _logger.LogInformation("Container detected via /proc/1/cgroup");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to read /proc/1/cgroup");
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the default temporary path based on platform and environment.
        /// </summary>
        private string GetDefaultTempPath()
        {
            if (_isRunningInContainer)
            {
                // Container: use /app/temp (matches volume mount in docker-compose)
                return "/app/temp";
            }

            if (_isWindows)
            {
                // Windows: use C:\Temp\YoutubeRag
                return @"C:\Temp\YoutubeRag";
            }

            // Linux/Mac: use /tmp/youtuberag
            return "/tmp/youtuberag";
        }

        /// <summary>
        /// Gets the default models path based on platform and environment.
        /// </summary>
        private string GetDefaultModelsPath()
        {
            if (_isRunningInContainer)
            {
                // Container: use /app/models
                return "/app/models";
            }

            if (_isWindows)
            {
                // Windows: use C:\Models\Whisper
                return @"C:\Models\Whisper";
            }

            // Linux/Mac: use /tmp/whisper-models
            return "/tmp/whisper-models";
        }

        /// <summary>
        /// Gets the default uploads path based on platform and environment.
        /// </summary>
        private string GetDefaultUploadsPath()
        {
            if (_isRunningInContainer)
            {
                // Container: use /app/uploads
                return "/app/uploads";
            }

            if (_isWindows)
            {
                // Windows: use C:\Uploads\YoutubeRag
                return @"C:\Uploads\YoutubeRag";
            }

            // Linux/Mac: use /tmp/uploads
            return "/tmp/uploads";
        }
    }
}
