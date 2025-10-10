namespace YoutubeRag.Application.DTOs.User;

/// <summary>
/// Public user profile data transfer object (no sensitive information)
/// </summary>
public record UserProfileDto
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
    /// Gets the user's avatar URL
    /// </summary>
    public string? Avatar { get; init; }

    /// <summary>
    /// Gets the user's bio/description
    /// </summary>
    public string? Bio { get; init; }

    /// <summary>
    /// Gets when the user account was created
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets the count of public videos
    /// </summary>
    public int PublicVideoCount { get; init; }

    /// <summary>
    /// Gets whether the user's email is verified
    /// </summary>
    public bool IsVerified { get; init; }
}