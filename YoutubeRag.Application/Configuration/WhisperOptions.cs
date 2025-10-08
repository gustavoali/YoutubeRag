namespace YoutubeRag.Application.Configuration;

/// <summary>
/// Configuration options for Whisper AI model management.
/// </summary>
public class WhisperOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "Whisper";

    /// <summary>
    /// The directory path where Whisper models are stored.
    /// Default: C:\Models\Whisper
    /// </summary>
    public string ModelsPath { get; set; } = "C:\\Models\\Whisper";

    /// <summary>
    /// The default model to use. Can be "auto" for automatic selection based on duration,
    /// or a specific model name (tiny, base, small).
    /// </summary>
    public string DefaultModel { get; set; } = "auto";

    /// <summary>
    /// Forces the use of a specific model regardless of video duration.
    /// When null, automatic selection is used. Valid values: tiny, base, small.
    /// </summary>
    public string? ForceModel { get; set; }

    /// <summary>
    /// Base URL for downloading Whisper models from OpenAI CDN.
    /// </summary>
    public string ModelDownloadUrl { get; set; } = "https://openaipublic.azureedge.net/main/whisper/models/";

    /// <summary>
    /// Number of days after which unused models are eligible for cleanup.
    /// Default: 30 days.
    /// </summary>
    public int CleanupUnusedModelsDays { get; set; } = 30;

    /// <summary>
    /// Minimum free disk space in GB required before downloading models.
    /// Default: 10 GB.
    /// </summary>
    public int MinDiskSpaceGB { get; set; } = 10;

    /// <summary>
    /// Number of retry attempts for model downloads.
    /// Default: 3.
    /// </summary>
    public int DownloadRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between download retry attempts in seconds.
    /// Default: 5 seconds.
    /// </summary>
    public int DownloadRetryDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Cache duration for the available models list in minutes.
    /// Default: 60 minutes.
    /// </summary>
    public int ModelCacheDurationMinutes { get; set; } = 60;

    /// <summary>
    /// Duration threshold in seconds for selecting 'tiny' model.
    /// Videos shorter than this will use tiny model.
    /// Default: 600 (10 minutes).
    /// </summary>
    public int TinyModelThresholdSeconds { get; set; } = 600;

    /// <summary>
    /// Duration threshold in seconds for selecting 'base' model.
    /// Videos shorter than this (but longer than tiny threshold) will use base model.
    /// Default: 1800 (30 minutes).
    /// </summary>
    public int BaseModelThresholdSeconds { get; set; } = 1800;
}
