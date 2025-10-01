using System.ComponentModel.DataAnnotations;

namespace YoutubeRag.Application.DTOs.Video;

/// <summary>
/// Data transfer object for updating video information
/// </summary>
public record UpdateVideoDto
{
    /// <summary>
    /// Gets the video title
    /// </summary>
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 255 characters")]
    public string? Title { get; init; }

    /// <summary>
    /// Gets the video description
    /// </summary>
    [StringLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
    public string? Description { get; init; }

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
    /// Gets whether to clear the description
    /// </summary>
    public bool? ClearDescription { get; init; }

    /// <summary>
    /// Gets whether to clear the thumbnail
    /// </summary>
    public bool? ClearThumbnail { get; init; }

    /// <summary>
    /// Gets the video status
    /// </summary>
    public YoutubeRag.Domain.Enums.VideoStatus? Status { get; init; }

    /// <summary>
    /// Gets the video duration
    /// </summary>
    public TimeSpan? Duration { get; init; }
}