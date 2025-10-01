using System.ComponentModel.DataAnnotations;

namespace YoutubeRag.Application.DTOs.Video;

/// <summary>
/// Data transfer object for creating a new video
/// </summary>
public record CreateVideoDto
{
    /// <summary>
    /// Gets the video title
    /// </summary>
    [Required(ErrorMessage = "Title is required")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 255 characters")]
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Gets the video description
    /// </summary>
    [StringLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
    public string? Description { get; init; }

    /// <summary>
    /// Gets the YouTube URL (required if not uploading a file)
    /// </summary>
    [Url(ErrorMessage = "Invalid YouTube URL format")]
    [StringLength(500, ErrorMessage = "URL cannot exceed 500 characters")]
    public string? YoutubeUrl { get; init; }

    /// <summary>
    /// Gets the thumbnail URL
    /// </summary>
    [Url(ErrorMessage = "Invalid thumbnail URL format")]
    [StringLength(500, ErrorMessage = "Thumbnail URL cannot exceed 500 characters")]
    public string? ThumbnailUrl { get; init; }

    /// <summary>
    /// Gets custom metadata as JSON
    /// </summary>
    public string? Metadata { get; init; }

    /// <summary>
    /// Gets whether to automatically start processing
    /// </summary>
    public bool AutoProcess { get; init; } = false;

    /// <summary>
    /// Gets the language for transcription
    /// </summary>
    [StringLength(10, ErrorMessage = "Language code cannot exceed 10 characters")]
    [RegularExpression(@"^[a-z]{2}(-[A-Z]{2})?$", ErrorMessage = "Invalid language code format (e.g., 'en' or 'en-US')")]
    public string? Language { get; init; } = "en";

    /// <summary>
    /// Validates that either YoutubeUrl or file upload is provided
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(YoutubeUrl);
    }
}