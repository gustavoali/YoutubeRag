namespace YoutubeRag.Domain.Enums;

/// <summary>
/// Represents the transcription status of a video
/// </summary>
public enum TranscriptionStatus
{
    /// <summary>
    /// Transcription has not been started
    /// </summary>
    NotStarted = 0,

    /// <summary>
    /// Transcription is pending
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Transcription is in progress
    /// </summary>
    InProgress = 2,

    /// <summary>
    /// Transcription completed successfully
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Transcription failed
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Transcription was skipped (e.g., no audio)
    /// </summary>
    Skipped = 5
}
