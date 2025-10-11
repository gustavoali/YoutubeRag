using System.ComponentModel.DataAnnotations;

namespace YoutubeRag.Application.DTOs.Auth;

/// <summary>
/// Registration request data transfer object
/// </summary>
public record RegisterRequestDto
{
    /// <summary>
    /// Gets the user's display name
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the user's email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Gets the user's password
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&].+$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number and one special character")]
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Gets the password confirmation
    /// </summary>
    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; init; } = string.Empty;

    /// <summary>
    /// Gets whether the user accepts the terms of service
    /// </summary>
    [Required(ErrorMessage = "You must accept the terms of service")]
    [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the terms of service")]
    public bool AcceptTerms { get; init; }

    /// <summary>
    /// Gets whether to subscribe to newsletter
    /// </summary>
    public bool SubscribeToNewsletter { get; init; } = false;

    /// <summary>
    /// Gets device information for initial login
    /// </summary>
    [StringLength(255, ErrorMessage = "Device info cannot exceed 255 characters")]
    public string? DeviceInfo { get; init; }
}
