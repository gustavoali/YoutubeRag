using YoutubeRag.Application.DTOs.User;

namespace YoutubeRag.Application.DTOs.Auth;

/// <summary>
/// Login response data transfer object
/// </summary>
public record LoginResponseDto
{
    /// <summary>
    /// Gets the access token (JWT)
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Gets the refresh token
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;

    /// <summary>
    /// Gets the token type (typically "Bearer")
    /// </summary>
    public string TokenType { get; init; } = "Bearer";

    /// <summary>
    /// Gets the access token expiry time in seconds
    /// </summary>
    public int ExpiresIn { get; init; }

    /// <summary>
    /// Gets the refresh token expiry time
    /// </summary>
    public DateTime RefreshTokenExpiresAt { get; init; }

    /// <summary>
    /// Gets the authenticated user information
    /// </summary>
    public UserDto User { get; init; } = new();

    /// <summary>
    /// Gets whether this is the first login
    /// </summary>
    public bool IsFirstLogin { get; init; }

    /// <summary>
    /// Gets whether two-factor authentication is required
    /// </summary>
    public bool RequiresTwoFactor { get; init; }

    /// <summary>
    /// Gets any additional login message
    /// </summary>
    public string? Message { get; init; }
}