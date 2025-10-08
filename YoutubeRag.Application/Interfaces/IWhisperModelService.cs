namespace YoutubeRag.Application.Interfaces;

/// <summary>
/// Service for managing Whisper AI models for audio transcription.
/// Handles model detection, download, selection, and lifecycle management.
/// </summary>
public interface IWhisperModelService
{
    /// <summary>
    /// Gets the file system path for a specific Whisper model.
    /// Downloads the model if not available locally.
    /// </summary>
    /// <param name="modelName">The name of the model (tiny, base, small, medium, large)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The absolute path to the model file</returns>
    /// <exception cref="InvalidOperationException">Thrown when model download fails or disk space is insufficient</exception>
    Task<string> GetModelPathAsync(string modelName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Automatically selects the optimal Whisper model based on video duration.
    /// Uses tiny for short videos, base for medium, small for long videos.
    /// </summary>
    /// <param name="durationSeconds">The duration of the video in seconds</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The name of the selected model</returns>
    Task<string> SelectModelForDurationAsync(int durationSeconds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific Whisper model is available locally.
    /// </summary>
    /// <param name="modelName">The name of the model to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the model exists and is valid, false otherwise</returns>
    Task<bool> IsModelAvailableAsync(string modelName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a list of all Whisper models currently available on the local file system.
    /// Results are cached to avoid repeated disk scans.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available model names</returns>
    Task<List<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Forces a refresh of the cached model list by rescanning the models directory.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RefreshModelCacheAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata about a specific model including size, last used date, and checksum.
    /// </summary>
    /// <param name="modelName">The name of the model</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Model metadata or null if model not found</returns>
    Task<WhisperModelMetadata?> GetModelMetadataAsync(string modelName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Metadata information about a Whisper model.
/// </summary>
public record WhisperModelMetadata
{
    /// <summary>
    /// The name of the model (tiny, base, small, etc.)
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The full file system path to the model file
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// The size of the model file in bytes
    /// </summary>
    public long SizeInBytes { get; init; }

    /// <summary>
    /// The last time this model was accessed or used
    /// </summary>
    public DateTime? LastUsedAt { get; init; }

    /// <summary>
    /// SHA256 checksum of the model file
    /// </summary>
    public string? Checksum { get; init; }

    /// <summary>
    /// Whether the model file exists and is accessible
    /// </summary>
    public bool IsAvailable { get; init; }
}
