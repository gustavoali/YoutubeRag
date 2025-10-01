using YoutubeRag.Application.DTOs.Auth;

namespace YoutubeRag.Application.Interfaces.Services;

/// <summary>
/// Service interface for authentication and authorization operations
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticate user with email and password
    /// </summary>
    Task<LoginResponseDto> LoginAsync(LoginRequestDto loginDto);

    /// <summary>
    /// Register a new user
    /// </summary>
    Task<LoginResponseDto> RegisterAsync(RegisterRequestDto registerDto);

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    Task<RefreshTokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto refreshDto);

    /// <summary>
    /// Logout user and invalidate refresh token
    /// </summary>
    Task LogoutAsync(string userId);

    /// <summary>
    /// Change user password
    /// </summary>
    Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequestDto changePasswordDto);

    /// <summary>
    /// Initiate password reset process
    /// </summary>
    Task<bool> ForgotPasswordAsync(ForgotPasswordRequestDto forgotPasswordDto);

    /// <summary>
    /// Complete password reset with token
    /// </summary>
    Task<bool> ResetPasswordAsync(ResetPasswordRequestDto resetPasswordDto);

    /// <summary>
    /// Verify user email with token
    /// </summary>
    Task<bool> VerifyEmailAsync(VerifyEmailRequestDto verifyEmailDto);

    /// <summary>
    /// Authenticate with Google OAuth
    /// </summary>
    Task<GoogleAuthResponseDto> GoogleAuthAsync(GoogleAuthRequestDto googleAuthDto);
}
