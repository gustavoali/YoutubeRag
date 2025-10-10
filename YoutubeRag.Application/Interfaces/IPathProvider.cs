using System;

namespace YoutubeRag.Application.Interfaces
{
    /// <summary>
    /// Provides cross-platform path resolution for file system operations.
    /// This service ensures consistent path handling across Windows, Linux, and container environments.
    /// </summary>
    /// <remarks>
    /// DEVOPS-002: Cross-Platform PathService
    ///
    /// Design Principles:
    /// - Platform-agnostic: Works seamlessly on Windows, Linux, and macOS
    /// - Container-friendly: Uses /app/* paths in containers, local paths otherwise
    /// - Configuration-first: Respects environment variables and appsettings.json
    /// - Fail-safe: Creates directories automatically if they don't exist
    ///
    /// Default Paths:
    /// - Windows Local: C:\Temp\YoutubeRag, C:\Models\Whisper, C:\Uploads\YoutubeRag
    /// - Linux/Mac Local: /tmp/youtuberag, /tmp/whisper-models, /tmp/uploads
    /// - Container: /app/temp, /app/models, /app/uploads
    ///
    /// Environment Variables (priority order):
    /// 1. PROCESSING_TEMP_PATH - Overrides temp path
    /// 2. WHISPER_MODELS_PATH - Overrides models path
    /// 3. UPLOAD_PATH - Overrides uploads path
    /// 4. Fallback to appsettings.json configuration
    /// 5. Fallback to platform-specific defaults
    /// </remarks>
    public interface IPathProvider
    {
        /// <summary>
        /// Gets the temporary file storage path for video/audio processing.
        /// </summary>
        /// <returns>
        /// Absolute path to temp directory. Creates directory if it doesn't exist.
        /// Windows: C:\Temp\YoutubeRag or PROCESSING_TEMP_PATH env var
        /// Linux: /tmp/youtuberag or PROCESSING_TEMP_PATH env var
        /// Container: /app/temp or PROCESSING_TEMP_PATH env var
        /// </returns>
        string GetTempPath();

        /// <summary>
        /// Gets the Whisper model storage path.
        /// </summary>
        /// <returns>
        /// Absolute path to models directory. Creates directory if it doesn't exist.
        /// Windows: C:\Models\Whisper or WHISPER_MODELS_PATH env var
        /// Linux: /tmp/whisper-models or WHISPER_MODELS_PATH env var
        /// Container: /app/models or WHISPER_MODELS_PATH env var
        /// </returns>
        string GetModelsPath();

        /// <summary>
        /// Gets the upload directory path for user-uploaded files.
        /// </summary>
        /// <returns>
        /// Absolute path to uploads directory. Creates directory if it doesn't exist.
        /// Windows: C:\Uploads\YoutubeRag or UPLOAD_PATH env var
        /// Linux: /tmp/uploads or UPLOAD_PATH env var
        /// Container: /app/uploads or UPLOAD_PATH env var
        /// </returns>
        string GetUploadsPath();

        /// <summary>
        /// Gets the logs directory path.
        /// </summary>
        /// <returns>
        /// Absolute path to logs directory. Creates directory if it doesn't exist.
        /// All platforms: ./logs relative to application root
        /// </returns>
        string GetLogsPath();

        /// <summary>
        /// Combines multiple path segments into a single normalized path.
        /// Uses platform-specific path separators.
        /// </summary>
        /// <param name="paths">Path segments to combine</param>
        /// <returns>Combined and normalized absolute path</returns>
        /// <example>
        /// Windows: CombinePath("C:\Temp", "video", "file.mp4") => "C:\Temp\video\file.mp4"
        /// Linux: CombinePath("/tmp", "video", "file.mp4") => "/tmp/video/file.mp4"
        /// </example>
        string CombinePath(params string[] paths);

        /// <summary>
        /// Normalizes a path to use platform-specific separators and format.
        /// Converts between Windows backslashes and Unix forward slashes.
        /// </summary>
        /// <param name="path">Path to normalize</param>
        /// <returns>Normalized path with platform-specific separators</returns>
        /// <example>
        /// Windows: NormalizePath("/tmp/file.txt") => "C:\tmp\file.txt" (if applicable)
        /// Linux: NormalizePath("C:\Temp\file.txt") => "/tmp/file.txt" (converted)
        /// </example>
        string NormalizePath(string path);

        /// <summary>
        /// Ensures a directory exists, creating it if necessary.
        /// Creates all parent directories in the path.
        /// </summary>
        /// <param name="path">Directory path to ensure exists</param>
        /// <returns>The same path (for method chaining)</returns>
        /// <exception cref="UnauthorizedAccessException">If lacking permissions to create directory</exception>
        /// <exception cref="IOException">If directory creation fails</exception>
        string EnsureDirectoryExists(string path);

        /// <summary>
        /// Gets a temporary file path with a unique name in the temp directory.
        /// </summary>
        /// <param name="extension">File extension (with or without leading dot)</param>
        /// <returns>Unique temporary file path (file not created, only path returned)</returns>
        /// <example>
        /// GetTempFilePath(".mp4") => "C:\Temp\YoutubeRag\abc123.mp4"
        /// GetTempFilePath("wav") => "/tmp/youtuberag/xyz789.wav"
        /// </example>
        string GetTempFilePath(string extension);

        /// <summary>
        /// Checks if the application is running in a Docker container.
        /// </summary>
        /// <returns>True if running in container, false otherwise</returns>
        bool IsRunningInContainer();

        /// <summary>
        /// Gets the platform-specific path separator (\ for Windows, / for Unix).
        /// </summary>
        /// <returns>Path separator character</returns>
        char GetPathSeparator();

        /// <summary>
        /// Resolves a path from configuration or environment variable, with fallback to default.
        /// Priority: Environment Variable > Configuration > Default
        /// </summary>
        /// <param name="environmentVariableName">Name of environment variable to check</param>
        /// <param name="configurationKey">Configuration key in appsettings.json</param>
        /// <param name="defaultPath">Fallback default path</param>
        /// <returns>Resolved absolute path</returns>
        string ResolvePath(string environmentVariableName, string configurationKey, string defaultPath);
    }
}
