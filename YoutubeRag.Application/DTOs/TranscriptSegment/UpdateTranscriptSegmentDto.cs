using System.ComponentModel.DataAnnotations;

namespace YoutubeRag.Application.DTOs.TranscriptSegment;

/// <summary>
/// Data transfer object for updating transcript segments
/// </summary>
public record UpdateTranscriptSegmentDto
{
    /// <summary>
    /// Gets the updated transcript text
    /// </summary>
    [MinLength(1, ErrorMessage = "Text cannot be empty")]
    public string? Text { get; init; }

    /// <summary>
    /// Gets the updated start time in seconds
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Start time must be non-negative")]
    public double? StartTime { get; init; }

    /// <summary>
    /// Gets the updated end time in seconds
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "End time must be non-negative")]
    public double? EndTime { get; init; }

    /// <summary>
    /// Gets the updated confidence score (0-1)
    /// </summary>
    [Range(0, 1, ErrorMessage = "Confidence must be between 0 and 1")]
    public double? Confidence { get; init; }

    /// <summary>
    /// Gets the updated language code
    /// </summary>
    [StringLength(10, ErrorMessage = "Language code cannot exceed 10 characters")]
    [RegularExpression(@"^[a-z]{2}(-[A-Z]{2})?$", ErrorMessage = "Invalid language code format (e.g., 'en' or 'en-US')")]
    public string? Language { get; init; }

    /// <summary>
    /// Validates that if both times are provided, end time is after start time
    /// </summary>
    public bool IsValid()
    {
        if (StartTime.HasValue && EndTime.HasValue)
        {
            return EndTime.Value > StartTime.Value;
        }

        return true;
    }
}
