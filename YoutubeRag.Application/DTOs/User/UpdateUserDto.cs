using System.ComponentModel.DataAnnotations;

namespace YoutubeRag.Application.DTOs.User;

/// <summary>
/// Data transfer object for updating user profile
/// </summary>
public record UpdateUserDto
{
    /// <summary>
    /// Gets the user's display name
    /// </summary>
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
    public string? Name { get; init; }

    /// <summary>
    /// Gets the user's bio/description
    /// </summary>
    [StringLength(500, ErrorMessage = "Bio cannot exceed 500 characters")]
    public string? Bio { get; init; }

    /// <summary>
    /// Gets the user's avatar URL
    /// </summary>
    [Url(ErrorMessage = "Invalid avatar URL format")]
    [StringLength(500, ErrorMessage = "Avatar URL cannot exceed 500 characters")]
    public string? Avatar { get; init; }

    /// <summary>
    /// Gets whether to remove the avatar
    /// </summary>
    public bool? RemoveAvatar { get; init; }

    /// <summary>
    /// Gets whether to activate or deactivate the user account
    /// </summary>
    public bool? IsActive { get; init; }

    /// <summary>
    /// Gets the user's email address
    /// </summary>
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
    public string? Email { get; init; }

    /// <summary>
    /// Gets whether the email is verified
    /// </summary>
    public bool? IsEmailVerified { get; init; }
}