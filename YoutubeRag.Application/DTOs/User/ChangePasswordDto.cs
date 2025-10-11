using System.ComponentModel.DataAnnotations;

namespace YoutubeRag.Application.DTOs.User;

/// <summary>
/// Data transfer object for changing user password
/// </summary>
public record ChangePasswordDto
{
    /// <summary>
    /// Gets the current password
    /// </summary>
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; init; } = string.Empty;

    /// <summary>
    /// Gets the new password
    /// </summary>
    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&].+$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number and one special character")]
    public string NewPassword { get; init; } = string.Empty;

    /// <summary>
    /// Gets the new password confirmation
    /// </summary>
    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
    public string ConfirmNewPassword { get; init; } = string.Empty;
}
