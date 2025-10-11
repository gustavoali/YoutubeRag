namespace YoutubeRag.Domain.Enums;

/// <summary>
/// Represents the status of embedding generation for a video's transcript segments
/// </summary>
public enum EmbeddingStatus
{
    /// <summary>
    /// No embeddings have been generated
    /// </summary>
    None = 0,

    /// <summary>
    /// Embedding generation is in progress
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// All embeddings have been successfully generated
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Embedding generation failed
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Partial embeddings generated (some segments succeeded, others failed)
    /// </summary>
    Partial = 4
}
