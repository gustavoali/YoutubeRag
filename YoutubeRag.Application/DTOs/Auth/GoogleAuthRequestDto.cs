using System.ComponentModel.DataAnnotations;

namespace YoutubeRag.Application.DTOs.Auth;

/// <summary>
/// Google OAuth authentication request data transfer object
/// </summary>
public record GoogleAuthRequestDto
{
    /// <summary>
    /// Gets the Google ID token
    /// </summary>
    [Required(ErrorMessage = "Google token is required")]
    public string GoogleToken { get; init; } = string.Empty;

    /// <summary>
    /// Gets device information for tracking
    /// </summary>
    [StringLength(255, ErrorMessage = "Device info cannot exceed 255 characters")]
    public string? DeviceInfo { get; init; }

    /// <summary>
    /// Gets whether to remember the user (longer token expiry)
    /// </summary>
    public bool RememberMe { get; init; } = false;
}
