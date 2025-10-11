using System.ComponentModel.DataAnnotations;

namespace YoutubeRag.Application.DTOs.Auth;

/// <summary>
/// Login request data transfer object
/// </summary>
public record LoginRequestDto
{
    /// <summary>
    /// Gets the user's email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Gets the user's password
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Gets whether to remember the user (longer token expiry)
    /// </summary>
    public bool RememberMe { get; init; } = false;

    /// <summary>
    /// Gets device information for tracking
    /// </summary>
    [StringLength(255, ErrorMessage = "Device info cannot exceed 255 characters")]
    public string? DeviceInfo { get; init; }
}
