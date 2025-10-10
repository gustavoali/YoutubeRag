namespace YoutubeRag.Application.DTOs.Transcription;

/// <summary>
/// Represents a request to transcribe audio content
/// </summary>
public record TranscriptionRequestDto(
    string VideoId,
    string AudioFilePath,
    string Language = "en",
    TranscriptionQuality Quality = TranscriptionQuality.Medium
);

/// <summary>
/// Defines the quality level for transcription processing
/// </summary>
public enum TranscriptionQuality
{
    /// <summary>
    /// Fastest processing with lower accuracy (tiny model)
    /// </summary>
    Low,

    /// <summary>
    /// Balanced speed and accuracy (base/small model)
    /// </summary>
    Medium,

    /// <summary>
    /// Highest accuracy but slower processing (medium/large model)
    /// </summary>
    High
}