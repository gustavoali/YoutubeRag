using System.ComponentModel.DataAnnotations;

namespace YoutubeRag.Application.DTOs.Auth;

/// <summary>
/// Forgot password request data transfer object
/// </summary>
public record ForgotPasswordRequestDto
{
    /// <summary>
    /// Gets the user's email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; init; } = string.Empty;
}
