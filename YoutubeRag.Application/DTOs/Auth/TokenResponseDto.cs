namespace YoutubeRag.Application.DTOs.Auth;

/// <summary>
/// Token response data transfer object
/// </summary>
public record TokenResponseDto
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
}
