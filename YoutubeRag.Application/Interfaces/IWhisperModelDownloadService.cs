namespace YoutubeRag.Application.Interfaces;

/// <summary>
/// Service for downloading and managing Whisper model files.
/// This is an infrastructure concern that handles file I/O and network operations.
/// </summary>
public interface IWhisperModelDownloadService
{
    /// <summary>
    /// Downloads a Whisper model from the configured CDN.
    /// Includes retry logic and progress reporting.
    /// </summary>
    /// <param name="modelName">The name of the model to download</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="InvalidOperationException">Thrown when download fails after all retries</exception>
    Task DownloadModelAsync(string modelName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the file system path where a model should be stored.
    /// </summary>
    /// <param name="modelName">The name of the model</param>
    /// <returns>The absolute file path for the model</returns>
    string GetModelFilePath(string modelName);

    /// <summary>
    /// Computes the SHA256 checksum of a model file.
    /// </summary>
    /// <param name="filePath">Path to the model file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The SHA256 checksum as a lowercase hex string</returns>
    Task<string> ComputeChecksumAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies that sufficient disk space is available for model downloads.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="InvalidOperationException">Thrown when insufficient disk space is available</exception>
    Task VerifyDiskSpaceAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes models that haven't been used within the configured cleanup period.
    /// Always keeps the 'tiny' model as a fallback.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of models deleted</returns>
    Task<int> CleanupUnusedModelsAsync(CancellationToken cancellationToken = default);
}
