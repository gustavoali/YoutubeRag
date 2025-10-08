using System.ComponentModel.DataAnnotations;

namespace YoutubeRag.Application.DTOs.Auth;

/// <summary>
/// Email verification request data transfer object
/// </summary>
public record VerifyEmailRequestDto
{
    /// <summary>
    /// Gets the email verification token
    /// </summary>
    [Required(ErrorMessage = "Verification token is required")]
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// Gets the user's email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; init; } = string.Empty;
}