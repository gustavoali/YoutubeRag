using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using YoutubeRag.Api.Configuration;

namespace YoutubeRag.Api.HealthChecks;

/// <summary>
/// Health check to verify Whisper models are available
/// </summary>
public class WhisperModelsHealthCheck : IHealthCheck
{
    private readonly ILogger<WhisperModelsHealthCheck> _logger;
    private readonly AppSettings _appSettings;

    // Common Whisper model file extensions
    private static readonly string[] ModelExtensions = { ".pt", ".bin", ".ggml", ".model" };

    // Common Whisper model directory paths
    private static readonly string[] ModelDirectories =
    {
        "models",
        "whisper_models",
        "./models",
        "./whisper_models",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache", "whisper"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "whisper", "models")
    };

    public WhisperModelsHealthCheck(
        ILogger<WhisperModelsHealthCheck> logger,
        IOptions<AppSettings> appSettings)
    {
        _logger = logger;
        _appSettings = appSettings.Value;
    }

    /// <summary>
    /// Checks if Whisper models are available in expected directories
    /// </summary>
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var modelsFound = new List<string>();
            var directoriesChecked = new List<string>();

            // Check each potential model directory
            foreach (var modelDir in ModelDirectories)
            {
                var fullPath = Path.GetFullPath(modelDir);
                directoriesChecked.Add(fullPath);

                if (!Directory.Exists(fullPath))
                {
                    continue;
                }

                // Search for model files
                var modelFiles = Directory.GetFiles(fullPath, "*.*", SearchOption.AllDirectories)
                    .Where(file => ModelExtensions.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                    .Select(Path.GetFileName)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Cast<string>()
                    .ToList();

                if (modelFiles.Any())
                {
                    modelsFound.AddRange(modelFiles);
                    _logger.LogDebug(
                        "Found {Count} Whisper model(s) in {Directory}: {Models}",
                        modelFiles.Count,
                        fullPath,
                        string.Join(", ", modelFiles));
                }
            }

            if (modelsFound.Any())
            {
                var configuredModelSize = _appSettings.WhisperModelSize?.ToLowerInvariant() ?? "medium";
                var hasConfiguredModel = modelsFound.Any(m =>
                    m.Contains(configuredModelSize, StringComparison.OrdinalIgnoreCase));

                if (hasConfiguredModel)
                {
                    _logger.LogDebug(
                        "Whisper health check passed. Found {Count} model(s) including configured model '{ModelSize}'",
                        modelsFound.Count,
                        configuredModelSize);

                    return Task.FromResult(HealthCheckResult.Healthy(
                        description: $"Whisper models available including '{configuredModelSize}'",
                        data: new Dictionary<string, object>
                        {
                            { "models_found", modelsFound.Count },
                            { "configured_model", configuredModelSize },
                            { "configured_model_available", true },
                            { "models", string.Join(", ", modelsFound.Distinct()) }
                        }));
                }
                else
                {
                    _logger.LogWarning(
                        "Whisper models found but configured model '{ModelSize}' is missing. Available: {Models}",
                        configuredModelSize,
                        string.Join(", ", modelsFound));

                    return Task.FromResult(HealthCheckResult.Degraded(
                        description: $"Configured Whisper model '{configuredModelSize}' not found",
                        data: new Dictionary<string, object>
                        {
                            { "models_found", modelsFound.Count },
                            { "configured_model", configuredModelSize },
                            { "configured_model_available", false },
                            { "available_models", string.Join(", ", modelsFound.Distinct()) },
                            { "warning", $"Model '{configuredModelSize}' not found. System may use fallback." }
                        }));
                }
            }
            else
            {
                _logger.LogWarning(
                    "Whisper health check failed: No models found in any checked directory. Checked: {Directories}",
                    string.Join(", ", directoriesChecked));

                return Task.FromResult(HealthCheckResult.Unhealthy(
                    description: "No Whisper models found",
                    data: new Dictionary<string, object>
                    {
                        { "models_found", 0 },
                        { "configured_model", _appSettings.WhisperModelSize ?? "medium" },
                        { "directories_checked", string.Join(", ", directoriesChecked) },
                        { "suggestion", "Download Whisper models or configure model directory path" },
                        { "expected_extensions", string.Join(", ", ModelExtensions) }
                    }));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Whisper models health check failed with unexpected error");

            return Task.FromResult(HealthCheckResult.Unhealthy(
                description: "Whisper models health check failed",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    { "error", ex.Message }
                }));
        }
    }
}
