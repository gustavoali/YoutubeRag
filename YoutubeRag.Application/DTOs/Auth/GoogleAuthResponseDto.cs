using YoutubeRag.Application.DTOs.User;

namespace YoutubeRag.Application.DTOs.Auth;

/// <summary>
/// DTO for Google OAuth response
/// </summary>
public record GoogleAuthResponseDto(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    int ExpiresIn,
    UserDto User
);
