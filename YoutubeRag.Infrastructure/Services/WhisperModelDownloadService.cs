using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YoutubeRag.Application.Configuration;
using YoutubeRag.Application.Interfaces;

namespace YoutubeRag.Infrastructure.Services;

/// <summary>
/// Infrastructure service for downloading and managing Whisper model files.
/// Handles file I/O, network operations, and disk space management.
/// </summary>
public class WhisperModelDownloadService : IWhisperModelDownloadService
{
    private readonly WhisperOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WhisperModelDownloadService> _logger;

    /// <summary>
    /// Model file names as they appear on the CDN
    /// </summary>
    private static readonly Dictionary<string, string> ModelFileNames = new()
    {
        { "tiny", "tiny.pt" },
        { "base", "base.pt" },
        { "small", "small.pt" }
    };

    /// <summary>
    /// Expected model file sizes (approximate, in bytes)
    /// Used for validation and disk space checks
    /// </summary>
    private static readonly Dictionary<string, long> ModelSizes = new()
    {
        { "tiny", 39_000_000 },     // ~39 MB
        { "base", 74_000_000 },     // ~74 MB
        { "small", 244_000_000 }    // ~244 MB
    };

    public WhisperModelDownloadService(
        IOptions<WhisperOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<WhisperModelDownloadService> logger)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Ensure models directory exists
        EnsureModelsDirectoryExists();
    }

    /// <inheritdoc />
    public async Task DownloadModelAsync(string modelName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelName);

        var normalizedModelName = modelName.ToLowerInvariant();

        if (!ModelFileNames.ContainsKey(normalizedModelName))
        {
            throw new ArgumentException($"Unknown model: {modelName}", nameof(modelName));
        }

        var filePath = GetModelFilePath(normalizedModelName);

        // If file already exists, verify it's valid
        if (File.Exists(filePath))
        {
            _logger.LogInformation("Model {ModelName} already exists at {Path}", modelName, filePath);

            try
            {
                await ValidateModelFileAsync(filePath, normalizedModelName, cancellationToken);
                _logger.LogInformation("Existing model {ModelName} is valid", modelName);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Existing model {ModelName} is corrupted, will re-download", modelName);
                File.Delete(filePath);
            }
        }

        // Verify disk space before download
        await VerifyDiskSpaceAsync(cancellationToken);

        // Download with retry logic
        var attempt = 0;
        var maxAttempts = _options.DownloadRetryAttempts;

        while (attempt < maxAttempts)
        {
            attempt++;

            try
            {
                _logger.LogInformation(
                    "Downloading model {ModelName} (attempt {Attempt}/{MaxAttempts})",
                    modelName,
                    attempt,
                    maxAttempts);

                await DownloadModelFileAsync(normalizedModelName, filePath, cancellationToken);

                // Validate the downloaded file
                await ValidateModelFileAsync(filePath, normalizedModelName, cancellationToken);

                _logger.LogInformation("Successfully downloaded and validated model {ModelName}", modelName);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                _logger.LogWarning(
                    ex,
                    "Download attempt {Attempt}/{MaxAttempts} failed for model {ModelName}, retrying in {Delay}s",
                    attempt,
                    maxAttempts,
                    modelName,
                    _options.DownloadRetryDelaySeconds);

                // Delete corrupted file if it exists
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                await Task.Delay(
                    TimeSpan.FromSeconds(_options.DownloadRetryDelaySeconds),
                    cancellationToken);
            }
            catch (Exception ex) when (attempt >= maxAttempts)
            {
                _logger.LogError(
                    ex,
                    "Failed to download model {ModelName} after {MaxAttempts} attempts",
                    modelName,
                    maxAttempts);

                // Clean up failed download
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                throw new InvalidOperationException(
                    $"Failed to download model '{modelName}' after {maxAttempts} attempts. See inner exception for details.",
                    ex);
            }
        }
    }

    /// <inheritdoc />
    public string GetModelFilePath(string modelName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelName);

        var normalizedModelName = modelName.ToLowerInvariant();
        var modelDir = Path.Combine(_options.ModelsPath, normalizedModelName);

        Directory.CreateDirectory(modelDir);

        var fileName = ModelFileNames.GetValueOrDefault(normalizedModelName, $"{normalizedModelName}.pt");
        return Path.Combine(modelDir, fileName);
    }

    /// <inheritdoc />
    public async Task<string> ComputeChecksumAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}", filePath);
        }

        using var sha256 = SHA256.Create();
        await using var stream = File.OpenRead(filePath);

        var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
        var hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();

        _logger.LogDebug("Computed checksum for {FilePath}: {Checksum}", filePath, hashString);

        return hashString;
    }

    /// <inheritdoc />
    public Task VerifyDiskSpaceAsync(CancellationToken cancellationToken = default)
    {
        var modelsDirectory = new DirectoryInfo(_options.ModelsPath);

        if (!modelsDirectory.Exists)
        {
            modelsDirectory.Create();
        }

        var drive = new DriveInfo(modelsDirectory.Root.FullName);

        var freeSpaceGB = drive.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0;
        var minRequiredGB = _options.MinDiskSpaceGB;

        _logger.LogInformation(
            "Disk space check: {FreeSpace:F2} GB free on drive {DriveName}",
            freeSpaceGB,
            drive.Name);

        if (freeSpaceGB < minRequiredGB)
        {
            var message = $"Insufficient disk space. Available: {freeSpaceGB:F2} GB, Required: {minRequiredGB} GB";
            _logger.LogError(message);
            throw new InvalidOperationException(message);
        }

        if (freeSpaceGB < minRequiredGB * 2)
        {
            _logger.LogWarning(
                "Low disk space warning: {FreeSpace:F2} GB available (recommended: {Recommended} GB)",
                freeSpaceGB,
                minRequiredGB * 2);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<int> CleanupUnusedModelsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting cleanup of unused Whisper models");

        var deletedCount = 0;
        var cutoffDate = DateTime.UtcNow.AddDays(-_options.CleanupUnusedModelsDays);

        foreach (var modelName in ModelFileNames.Keys)
        {
            // Never delete the 'tiny' model - keep it as a fallback
            if (modelName == "tiny")
            {
                _logger.LogDebug("Skipping cleanup for 'tiny' model (always kept as fallback)");
                continue;
            }

            var filePath = GetModelFilePath(modelName);

            if (!File.Exists(filePath))
            {
                continue;
            }

            var fileInfo = new FileInfo(filePath);

            if (fileInfo.LastAccessTime < cutoffDate)
            {
                try
                {
                    _logger.LogInformation(
                        "Deleting unused model {ModelName} (last used: {LastUsed:yyyy-MM-dd})",
                        modelName,
                        fileInfo.LastAccessTime);

                    File.Delete(filePath);

                    // Also delete the model directory if it's empty
                    var modelDir = Path.GetDirectoryName(filePath);
                    if (modelDir != null && Directory.Exists(modelDir) && !Directory.EnumerateFileSystemEntries(modelDir).Any())
                    {
                        Directory.Delete(modelDir);
                    }

                    deletedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete unused model {ModelName}", modelName);
                }
            }
        }

        _logger.LogInformation("Cleanup complete. Deleted {Count} unused models", deletedCount);

        return deletedCount;
    }

    /// <summary>
    /// Downloads the model file from the CDN with progress reporting.
    /// </summary>
    private async Task DownloadModelFileAsync(
        string modelName,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        var fileName = ModelFileNames[modelName];
        var url = $"{_options.ModelDownloadUrl}{fileName}";

        _logger.LogInformation("Downloading from {Url} to {Path}", url, destinationPath);

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromMinutes(30); // Large models can take time

        using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        var downloadedBytes = 0L;

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(
            destinationPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920, // 80KB buffer
            useAsync: true);

        var buffer = new byte[81920];
        int bytesRead;
        var lastLoggedProgress = 0.0;

        while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            downloadedBytes += bytesRead;

            if (totalBytes > 0)
            {
                var progressPercent = (double)downloadedBytes / totalBytes * 100;

                // Log progress every 10%
                if (progressPercent - lastLoggedProgress >= 10)
                {
                    _logger.LogInformation(
                        "Downloading {ModelName}: {Progress:F1}% ({Downloaded} MB / {Total} MB)",
                        modelName,
                        progressPercent,
                        downloadedBytes / 1024.0 / 1024.0,
                        totalBytes / 1024.0 / 1024.0);

                    lastLoggedProgress = progressPercent;
                }
            }
        }

        _logger.LogInformation(
            "Download complete for {ModelName}: {Size} MB",
            modelName,
            downloadedBytes / 1024.0 / 1024.0);
    }

    /// <summary>
    /// Validates a downloaded model file by checking size and computing checksum.
    /// </summary>
    private async Task ValidateModelFileAsync(
        string filePath,
        string modelName,
        CancellationToken cancellationToken)
    {
        var fileInfo = new FileInfo(filePath);

        // Verify file size is reasonable
        var expectedSize = ModelSizes.GetValueOrDefault(modelName, 0);
        var sizeDifference = Math.Abs(fileInfo.Length - expectedSize);
        var sizeTolerancePercent = 0.20; // 20% tolerance

        if (expectedSize > 0 && sizeDifference > expectedSize * sizeTolerancePercent)
        {
            throw new InvalidOperationException(
                $"Model file size mismatch. Expected: ~{expectedSize / 1024.0 / 1024.0:F1} MB, " +
                $"Actual: {fileInfo.Length / 1024.0 / 1024.0:F1} MB");
        }

        // Compute and log checksum
        var checksum = await ComputeChecksumAsync(filePath, cancellationToken);

        _logger.LogInformation(
            "Model {ModelName} validated - Size: {Size} MB, Checksum: {Checksum}",
            modelName,
            fileInfo.Length / 1024.0 / 1024.0,
            checksum);
    }

    /// <summary>
    /// Ensures the models directory structure exists.
    /// </summary>
    private void EnsureModelsDirectoryExists()
    {
        if (!Directory.Exists(_options.ModelsPath))
        {
            _logger.LogInformation("Creating models directory: {Path}", _options.ModelsPath);
            Directory.CreateDirectory(_options.ModelsPath);
        }
    }
}
