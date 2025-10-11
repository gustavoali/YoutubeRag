namespace YoutubeRag.Application.DTOs.User;

/// <summary>
/// Full user data transfer object for API responses
/// </summary>
public record UserDto
{
    /// <summary>
    /// Gets the user's unique identifier
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets the user's display name
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the user's email address
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Gets whether the user account is active
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Gets whether the user's email is verified
    /// </summary>
    public bool IsEmailVerified { get; init; }

    /// <summary>
    /// Gets the date when the email was verified
    /// </summary>
    public DateTime? EmailVerifiedAt { get; init; }

    /// <summary>
    /// Gets the user's avatar URL
    /// </summary>
    public string? Avatar { get; init; }

    /// <summary>
    /// Gets the user's bio/description
    /// </summary>
    public string? Bio { get; init; }

    /// <summary>
    /// Gets whether the user has Google OAuth linked
    /// </summary>
    public bool HasGoogleAuth { get; init; }

    /// <summary>
    /// Gets the last login timestamp
    /// </summary>
    public DateTime? LastLoginAt { get; init; }

    /// <summary>
    /// Gets when the user account was created
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets when the user account was last updated
    /// </summary>
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Gets the count of videos owned by the user
    /// </summary>
    public int VideoCount { get; init; }

    /// <summary>
    /// Gets the count of jobs created by the user
    /// </summary>
    public int JobCount { get; init; }
}
