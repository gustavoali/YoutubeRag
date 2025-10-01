namespace YoutubeRag.Application.DTOs.Auth;

/// <summary>
/// DTO for refresh token response
/// </summary>
public record RefreshTokenResponseDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public string TokenType { get; init; } = "Bearer";
    public int ExpiresIn { get; init; }
}
