using System.ComponentModel.DataAnnotations;

namespace YoutubeRag.Application.DTOs.TranscriptSegment;

/// <summary>
/// Data transfer object for creating transcript segments
/// </summary>
public record CreateTranscriptSegmentDto
{
    /// <summary>
    /// Gets the video ID this segment belongs to
    /// </summary>
    [Required(ErrorMessage = "Video ID is required")]
    public string VideoId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the transcript text
    /// </summary>
    [Required(ErrorMessage = "Text is required")]
    [MinLength(1, ErrorMessage = "Text cannot be empty")]
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// Gets the start time in seconds
    /// </summary>
    [Required(ErrorMessage = "Start time is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Start time must be non-negative")]
    public double StartTime { get; init; }

    /// <summary>
    /// Gets the end time in seconds
    /// </summary>
    [Required(ErrorMessage = "End time is required")]
    [Range(0, double.MaxValue, ErrorMessage = "End time must be non-negative")]
    public double EndTime { get; init; }

    /// <summary>
    /// Gets the segment index
    /// </summary>
    [Required(ErrorMessage = "Segment index is required")]
    [Range(0, int.MaxValue, ErrorMessage = "Segment index must be non-negative")]
    public int SegmentIndex { get; init; }

    /// <summary>
    /// Gets the confidence score (0-1)
    /// </summary>
    [Range(0, 1, ErrorMessage = "Confidence must be between 0 and 1")]
    public double? Confidence { get; init; }

    /// <summary>
    /// Gets the language code
    /// </summary>
    [StringLength(10, ErrorMessage = "Language code cannot exceed 10 characters")]
    [RegularExpression(@"^[a-z]{2}(-[A-Z]{2})?$", ErrorMessage = "Invalid language code format (e.g., 'en' or 'en-US')")]
    public string? Language { get; init; }

    /// <summary>
    /// Validates that end time is after start time
    /// </summary>
    public bool IsValid()
    {
        return EndTime > StartTime;
    }
}
