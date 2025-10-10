namespace YoutubeRag.Application.DTOs.Auth;

/// <summary>
/// DTO for password change request
/// </summary>
public record ChangePasswordRequestDto(
    string CurrentPassword,
    string NewPassword
);
