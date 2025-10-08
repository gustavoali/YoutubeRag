namespace YoutubeRag.Domain.Enums;

/// <summary>
/// Represents the status of a video in the processing pipeline
/// </summary>
public enum VideoStatus
{
    /// <summary>
    /// Video has been created but processing has not started
    /// </summary>
    Pending,

    /// <summary>
    /// Video metadata has been extracted from YouTube
    /// </summary>
    MetadataExtracted,

    /// <summary>
    /// Video is being downloaded from YouTube
    /// </summary>
    Downloading,

    /// <summary>
    /// Audio has been successfully extracted from the video
    /// </summary>
    AudioExtracted,

    /// <summary>
    /// Video is being processed (transcription, embedding, etc.)
    /// </summary>
    Processing,

    /// <summary>
    /// All processing stages completed successfully
    /// </summary>
    Completed,

    /// <summary>
    /// Processing failed with an error
    /// </summary>
    Failed,

    /// <summary>
    /// Processing was cancelled by the user
    /// </summary>
    Cancelled
}
