namespace YoutubeRag.Application.DTOs.Transcription;

/// <summary>
/// Represents the result of a transcription operation
/// </summary>
public class TranscriptionResultDto
{
    /// <summary>
    /// The ID of the video that was transcribed
    /// </summary>
    public string VideoId { get; set; } = string.Empty;

    /// <summary>
    /// List of transcript segments with timestamps
    /// </summary>
    public List<TranscriptSegmentDto> Segments { get; set; } = new();

    /// <summary>
    /// Total duration of the transcribed audio
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Detected or specified language of the transcription
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Overall confidence score of the transcription (0-1)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// The Whisper model used for transcription (e.g., "tiny", "base", "small", "medium")
    /// </summary>
    public string? ModelUsed { get; set; }

    /// <summary>
    /// Indicates if the model was downgraded due to out-of-memory errors
    /// </summary>
    public bool QualityDegraded { get; set; }

    /// <summary>
    /// The original model that was attempted before any downgrades (if applicable)
    /// </summary>
    public string? OriginalModel { get; set; }

    /// <summary>
    /// Number of retry attempts made during transcription
    /// </summary>
    public int RetryAttempts { get; set; }

    /// <summary>
    /// Full concatenated transcript text
    /// </summary>
    public string FullText => string.Join(" ", Segments.Select(s => s.Text));

    /// <summary>
    /// Total word count across all segments
    /// </summary>
    public int WordCount => FullText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
}

/// <summary>
/// Represents a single segment of transcribed text with timing information
/// </summary>
public class TranscriptSegmentDto
{
    /// <summary>
    /// Start time of the segment in seconds
    /// </summary>
    public double StartTime { get; set; }

    /// <summary>
    /// End time of the segment in seconds
    /// </summary>
    public double EndTime { get; set; }

    /// <summary>
    /// Transcribed text for this segment
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score for this segment (0-1)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Optional speaker identification
    /// </summary>
    public string? Speaker { get; set; }

    /// <summary>
    /// Duration of this segment in seconds
    /// </summary>
    public double Duration => EndTime - StartTime;
}
