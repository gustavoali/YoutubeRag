using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YoutubeRag.Application.Configuration;
using YoutubeRag.Application.Interfaces;

namespace YoutubeRag.Application.Services;

/// <summary>
/// Manages Whisper AI models including selection, caching, and coordination with download service.
/// This service handles business logic for model selection and lifecycle management.
/// </summary>
public class WhisperModelManager : IWhisperModelService
{
    private readonly WhisperOptions _options;
    private readonly IWhisperModelDownloadService _downloadService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<WhisperModelManager> _logger;

    private const string CacheKeyPrefix = "WhisperModels_";
    private const string AvailableModelsCacheKey = "WhisperModels_Available";

    /// <summary>
    /// Supported Whisper models for MVP (tiny, base, small only)
    /// </summary>
    private static readonly string[] SupportedModels = { "tiny", "base", "small" };

    public WhisperModelManager(
        IOptions<WhisperOptions> options,
        IWhisperModelDownloadService downloadService,
        IMemoryCache cache,
        ILogger<WhisperModelManager> logger)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _downloadService = downloadService ?? throw new ArgumentNullException(nameof(downloadService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<string> GetModelPathAsync(string modelName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelName);

        if (!SupportedModels.Contains(modelName.ToLowerInvariant()))
        {
            throw new ArgumentException(
                $"Unsupported model '{modelName}'. Supported models: {string.Join(", ", SupportedModels)}",
                nameof(modelName));
        }

        _logger.LogInformation("Getting path for Whisper model: {ModelName}", modelName);

        // Check if model is already available
        var isAvailable = await IsModelAvailableAsync(modelName, cancellationToken);

        if (!isAvailable)
        {
            _logger.LogInformation("Model {ModelName} not available locally, initiating download", modelName);

            // Verify disk space before download
            await _downloadService.VerifyDiskSpaceAsync(cancellationToken);

            // Download the model
            await _downloadService.DownloadModelAsync(modelName, cancellationToken);

            // Invalidate cache after download
            await RefreshModelCacheAsync(cancellationToken);
        }

        var modelPath = _downloadService.GetModelFilePath(modelName);

        _logger.LogInformation("Model {ModelName} available at: {ModelPath}", modelName, modelPath);

        return modelPath;
    }

    /// <inheritdoc />
    public Task<string> SelectModelForDurationAsync(int durationSeconds, CancellationToken cancellationToken = default)
    {
        if (durationSeconds < 0)
        {
            throw new ArgumentException("Duration cannot be negative", nameof(durationSeconds));
        }

        // If a specific model is forced, use it
        if (!string.IsNullOrWhiteSpace(_options.ForceModel))
        {
            var forcedModel = _options.ForceModel.ToLowerInvariant();
            _logger.LogInformation(
                "Using forced model: {Model} (video duration: {Duration}s)",
                forcedModel,
                durationSeconds);

            return Task.FromResult(forcedModel);
        }

        // Automatic model selection based on duration
        var selectedModel = durationSeconds switch
        {
            < 600 => "tiny",    // < 10 minutes: tiny (39 MB, ~10x realtime)
            < 1800 => "base",   // < 30 minutes: base (74 MB, ~7x realtime)
            _ => "small"        // >= 30 minutes: small (244 MB, ~4x realtime)
        };

        // Use configurable thresholds if different from defaults
        if (_options.TinyModelThresholdSeconds != 600 || _options.BaseModelThresholdSeconds != 1800)
        {
            selectedModel = durationSeconds switch
            {
                _ when durationSeconds < _options.TinyModelThresholdSeconds => "tiny",
                _ when durationSeconds < _options.BaseModelThresholdSeconds => "base",
                _ => "small"
            };
        }

        _logger.LogInformation(
            "Auto-selected model: {Model} for video duration: {Duration}s ({Minutes:F1} min)",
            selectedModel,
            durationSeconds,
            durationSeconds / 60.0);

        return Task.FromResult(selectedModel);
    }

    /// <inheritdoc />
    public Task<bool> IsModelAvailableAsync(string modelName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelName);

        var modelPath = _downloadService.GetModelFilePath(modelName);
        var isAvailable = File.Exists(modelPath);

        _logger.LogDebug("Model {ModelName} availability check: {IsAvailable}", modelName, isAvailable);

        return Task.FromResult(isAvailable);
    }

    /// <inheritdoc />
    public async Task<List<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        // Try to get from cache first
        if (_cache.TryGetValue<List<string>>(AvailableModelsCacheKey, out var cachedModels))
        {
            _logger.LogDebug("Returning cached list of {Count} available models", cachedModels?.Count ?? 0);
            return cachedModels ?? new List<string>();
        }

        _logger.LogDebug("Scanning for available Whisper models");

        var availableModels = new List<string>();

        foreach (var modelName in SupportedModels)
        {
            if (await IsModelAvailableAsync(modelName, cancellationToken))
            {
                availableModels.Add(modelName);
            }
        }

        // Cache the result
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(_options.ModelCacheDurationMinutes));

        _cache.Set(AvailableModelsCacheKey, availableModels, cacheOptions);

        _logger.LogInformation(
            "Found {Count} available models: {Models}",
            availableModels.Count,
            string.Join(", ", availableModels));

        return availableModels;
    }

    /// <inheritdoc />
    public Task RefreshModelCacheAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Refreshing model cache");

        _cache.Remove(AvailableModelsCacheKey);

        // Remove individual model metadata cache entries
        foreach (var modelName in SupportedModels)
        {
            _cache.Remove($"{CacheKeyPrefix}{modelName}");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<WhisperModelMetadata?> GetModelMetadataAsync(
        string modelName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelName);

        var cacheKey = $"{CacheKeyPrefix}{modelName}";

        // Try to get from cache
        if (_cache.TryGetValue<WhisperModelMetadata>(cacheKey, out var cachedMetadata))
        {
            return cachedMetadata;
        }

        var modelPath = _downloadService.GetModelFilePath(modelName);

        if (!File.Exists(modelPath))
        {
            _logger.LogWarning("Model {ModelName} not found at path: {Path}", modelName, modelPath);
            return null;
        }

        var fileInfo = new FileInfo(modelPath);

        var metadata = new WhisperModelMetadata
        {
            Name = modelName,
            FilePath = modelPath,
            SizeInBytes = fileInfo.Length,
            LastUsedAt = fileInfo.LastAccessTime,
            IsAvailable = true,
            Checksum = await _downloadService.ComputeChecksumAsync(modelPath, cancellationToken)
        };

        // Cache the metadata
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(_options.ModelCacheDurationMinutes));

        _cache.Set(cacheKey, metadata, cacheOptions);

        _logger.LogDebug(
            "Retrieved metadata for model {ModelName}: {Size} bytes, last used: {LastUsed}",
            modelName,
            metadata.SizeInBytes,
            metadata.LastUsedAt);

        return metadata;
    }
}
