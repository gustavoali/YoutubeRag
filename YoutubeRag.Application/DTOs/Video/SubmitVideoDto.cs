using System.ComponentModel.DataAnnotations;

namespace YoutubeRag.Application.DTOs.Video;

/// <summary>
/// Data transfer object for submitting a YouTube video URL for processing
/// </summary>
public record SubmitVideoDto
{
    /// <summary>
    /// Gets the YouTube video URL (supports youtube.com and youtu.be formats)
    /// </summary>
    [Required(ErrorMessage = "URL is required")]
    [StringLength(2048, MinimumLength = 1, ErrorMessage = "URL must be between 1 and 2048 characters")]
    [RegularExpression(@"^https?://.*", ErrorMessage = "URL must start with http:// or https://")]
    public string Url { get; init; } = string.Empty;
}
