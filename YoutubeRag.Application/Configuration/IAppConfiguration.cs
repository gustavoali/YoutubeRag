namespace YoutubeRag.Application.Configuration;

/// <summary>
/// Application configuration interface
/// </summary>
public interface IAppConfiguration
{
    /// <summary>
    /// Current application environment (Local, Development, Production, etc.)
    /// </summary>
    string Environment { get; }

    /// <summary>
    /// Path to store extracted audio files
    /// </summary>
    string AudioStoragePath { get; }

    /// <summary>
    /// Whisper model size to use for transcription
    /// </summary>
    string WhisperModelSize { get; }

    /// <summary>
    /// Whether to automatically transcribe videos after ingestion
    /// </summary>
    bool AutoTranscribe { get; }

    /// <summary>
    /// Maximum audio file size in MB
    /// </summary>
    int MaxAudioFileSizeMB { get; }

    /// <summary>
    /// Dimension of embedding vectors
    /// </summary>
    int EmbeddingDimension { get; }

    /// <summary>
    /// Batch size for embedding generation
    /// </summary>
    int EmbeddingBatchSize { get; }

    /// <summary>
    /// Whether to automatically generate embeddings after transcription
    /// </summary>
    bool AutoGenerateEmbeddings { get; }

    /// <summary>
    /// Maximum segment length for text segmentation
    /// </summary>
    int MaxSegmentLength { get; }

    /// <summary>
    /// Minimum segment length for text segmentation
    /// </summary>
    int MinSegmentLength { get; }

    /// <summary>
    /// Whether to automatically downgrade Whisper model on out-of-memory errors
    /// </summary>
    bool EnableAutoModelDowngrade { get; }

    /// <summary>
    /// Path to store temporary files (videos, intermediate audio)
    /// </summary>
    string? TempFilePath { get; }

    /// <summary>
    /// Hours after which temporary files should be cleaned up (default: 24)
    /// </summary>
    int? CleanupAfterHours { get; }

    /// <summary>
    /// Minimum disk space in GB to maintain (default: 5)
    /// </summary>
    int? MinDiskSpaceGB { get; }
}
