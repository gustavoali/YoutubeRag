using System.ComponentModel.DataAnnotations;

namespace YoutubeRag.Application.DTOs.Auth;

/// <summary>
/// Refresh token request data transfer object
/// </summary>
public record RefreshTokenRequestDto
{
    /// <summary>
    /// Gets the refresh token
    /// </summary>
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; init; } = string.Empty;

    /// <summary>
    /// Gets device information for tracking
    /// </summary>
    [StringLength(255, ErrorMessage = "Device info cannot exceed 255 characters")]
    public string? DeviceInfo { get; init; }
}