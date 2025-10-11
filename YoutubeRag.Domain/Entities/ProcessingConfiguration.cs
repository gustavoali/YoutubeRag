namespace YoutubeRag.Domain.Entities;

/// <summary>
/// Represents configuration settings for video processing pipeline
/// </summary>
public class ProcessingConfiguration : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool UseLocalWhisper { get; set; } = true;
    public bool UseLocalEmbeddings { get; set; } = true;
    public int MaxConcurrentJobs { get; set; } = 3;
    public int RetryAttempts { get; set; } = 3;
    public int TimeoutMinutes { get; set; } = 30;
    public bool IsActive { get; set; } = true;

    // Whisper Configuration
    public string? WhisperModel { get; set; } = "base"; // tiny, base, small, medium, large
    public string? WhisperLanguage { get; set; } = "auto";

    // Embedding Configuration
    public string? EmbeddingModel { get; set; } = "all-MiniLM-L6-v2";
    public int ChunkSize { get; set; } = 500;
    public int ChunkOverlap { get; set; } = 50;

    // Queue Configuration
    public string? DefaultQueue { get; set; } = "default";
    public int Priority { get; set; } = 0;

    // Additional Settings as JSON
    public string? AdditionalSettings { get; set; }
}
