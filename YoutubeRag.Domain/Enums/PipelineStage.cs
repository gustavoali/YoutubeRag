namespace YoutubeRag.Domain.Enums;

/// <summary>
/// Represents the current stage in the transcription pipeline
/// </summary>
public enum PipelineStage
{
    /// <summary>
    /// Initial state - no stage started
    /// </summary>
    None = 0,

    /// <summary>
    /// Downloading video from YouTube
    /// </summary>
    Download = 1,

    /// <summary>
    /// Extracting audio from downloaded video
    /// </summary>
    AudioExtraction = 2,

    /// <summary>
    /// Transcribing audio using Whisper
    /// </summary>
    Transcription = 3,

    /// <summary>
    /// Storing transcript segments in database
    /// </summary>
    Segmentation = 4,

    /// <summary>
    /// Pipeline completed successfully
    /// </summary>
    Completed = 5
}
