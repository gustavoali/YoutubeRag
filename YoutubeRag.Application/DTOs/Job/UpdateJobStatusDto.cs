using System.ComponentModel.DataAnnotations;

namespace YoutubeRag.Application.DTOs.Job;

/// <summary>
/// Data transfer object for updating job status
/// </summary>
public record UpdateJobStatusDto
{
    /// <summary>
    /// Gets the new status
    /// </summary>
    [Required(ErrorMessage = "Status is required")]
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Gets the status message
    /// </summary>
    [StringLength(500, ErrorMessage = "Status message cannot exceed 500 characters")]
    public string? StatusMessage { get; init; }

    /// <summary>
    /// Gets the progress percentage (0-100)
    /// </summary>
    [Range(0, 100, ErrorMessage = "Progress must be between 0 and 100")]
    public int? Progress { get; init; }

    /// <summary>
    /// Gets the error message if job failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the result data if job completed
    /// </summary>
    public string? Result { get; init; }

    /// <summary>
    /// Gets updated metadata
    /// </summary>
    public string? Metadata { get; init; }
}
