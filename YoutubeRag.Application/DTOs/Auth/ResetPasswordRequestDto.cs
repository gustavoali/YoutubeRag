using System.ComponentModel.DataAnnotations;

namespace YoutubeRag.Application.DTOs.Auth;

/// <summary>
/// Reset password request data transfer object
/// </summary>
public record ResetPasswordRequestDto
{
    /// <summary>
    /// Gets the password reset token
    /// </summary>
    [Required(ErrorMessage = "Reset token is required")]
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// Gets the user's email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Gets the new password
    /// </summary>
    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&].+$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number and one special character")]
    public string NewPassword { get; init; } = string.Empty;

    /// <summary>
    /// Gets the password confirmation
    /// </summary>
    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; init; } = string.Empty;
}
