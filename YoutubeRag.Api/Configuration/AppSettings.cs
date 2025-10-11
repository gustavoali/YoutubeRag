namespace YoutubeRag.Api.Configuration;

public class AppSettings
{
    public const string SectionName = "AppSettings";

    public string Environment { get; set; } = "Development";
    public string ProcessingMode { get; set; } = "Mock";
    public string StorageMode { get; set; } = "Database";
    public bool EnableAuth { get; set; } = true;
    public bool EnableWebSockets { get; set; } = true;
    public bool EnableMetrics { get; set; } = true;
    public bool EnableRealProcessing { get; set; } = false;
    public bool EnableDocs { get; set; } = true;
    public bool EnableCors { get; set; } = true;

    // Hangfire settings
    public bool EnableBackgroundJobs { get; set; } = true;
    public int? MaxConcurrentJobs { get; set; } = 3;
    public bool EnableHangfireDashboard { get; set; } = true;

    // Audio and Transcription settings
    public string AudioStoragePath { get; set; } = "./data/audio";
    public string WhisperModelSize { get; set; } = "medium";
    public bool AutoTranscribe { get; set; } = true;
    public int MaxAudioFileSizeMB { get; set; } = 500;
    public bool EnableAutoModelDowngrade { get; set; } = true;

    // Embedding settings
    public int EmbeddingDimension { get; set; } = 384;
    public int EmbeddingBatchSize { get; set; } = 32;
    public bool AutoGenerateEmbeddings { get; set; } = true;
    public int MaxSegmentLength { get; set; } = 500;
    public int MinSegmentLength { get; set; } = 100;

    // Temp file management settings
    public string? TempFilePath { get; set; }
    public int? CleanupAfterHours { get; set; } = 24;
    public int? MinDiskSpaceGB { get; set; } = 5;

    // Helper properties
    public bool IsDevelopment => Environment.Equals("Development", StringComparison.OrdinalIgnoreCase);
    public bool IsProduction => Environment.Equals("Production", StringComparison.OrdinalIgnoreCase);
    public bool IsTesting => Environment.Equals("Testing", StringComparison.OrdinalIgnoreCase);

    public bool UseMockProcessing => ProcessingMode.Equals("Mock", StringComparison.OrdinalIgnoreCase);
    public bool UseRealProcessing => ProcessingMode.Equals("Real", StringComparison.OrdinalIgnoreCase);
    public bool UseHybridProcessing => ProcessingMode.Equals("Hybrid", StringComparison.OrdinalIgnoreCase);

    public bool UseMemoryStorage => StorageMode.Equals("Memory", StringComparison.OrdinalIgnoreCase);
    public bool UseDatabaseStorage => StorageMode.Equals("Database", StringComparison.OrdinalIgnoreCase);
    public bool UseHybridStorage => StorageMode.Equals("Hybrid", StringComparison.OrdinalIgnoreCase);
}

public class RateLimitingSettings
{
    public const string SectionName = "RateLimiting";

    public int PermitLimit { get; set; } = 100;
    public int WindowMinutes { get; set; } = 1;
}
