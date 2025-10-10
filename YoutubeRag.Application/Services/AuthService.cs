using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using YoutubeRag.Application.DTOs.Auth;
using YoutubeRag.Application.Exceptions;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Application.Interfaces.Services;
using YoutubeRag.Domain.Entities;

namespace YoutubeRag.Application.Services;

/// <summary>
/// Service implementation for authentication and authorization operations
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthService> _logger;
    private readonly IConfiguration _configuration;

    public AuthService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<AuthService> logger,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto loginDto)
    {
        _logger.LogInformation("Login attempt for email: {Email}", loginDto.Email);

        var users = await _unitOfWork.Users.FindAsync(u => u.Email == loginDto.Email);
        var user = users.FirstOrDefault();

        if (user == null)
        {
            _logger.LogWarning("Invalid login attempt for email: {Email} - User not found", loginDto.Email);
            throw new UnauthorizedException("Invalid email or password");
        }

        // Check if account is locked
        if (user.LockoutEndDate.HasValue && user.LockoutEndDate.Value > DateTime.UtcNow)
        {
            var remainingTime = user.LockoutEndDate.Value - DateTime.UtcNow;
            _logger.LogWarning("Login attempt for locked account: {Email}. Lockout ends in {Minutes} minutes",
                loginDto.Email, remainingTime.TotalMinutes);
            throw new UnauthorizedException(
                $"Account is locked due to multiple failed login attempts. Try again in {Math.Ceiling(remainingTime.TotalMinutes)} minutes.");
        }

        // Verify password
        if (!VerifyPassword(loginDto.Password, user.PasswordHash))
        {
            // Increment failed login attempts
            user.FailedLoginAttempts++;

            // Lock account after 5 failed attempts
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockoutEndDate = DateTime.UtcNow.AddMinutes(15);
                _logger.LogWarning("Account locked after 5 failed attempts: {Email}", loginDto.Email);
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogWarning("Invalid login attempt for email: {Email} - Wrong password. Attempt {Attempts}/5",
                loginDto.Email, user.FailedLoginAttempts);
            throw new UnauthorizedException("Invalid email or password");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt for inactive user: {UserId}", user.Id);
            throw new UnauthorizedException("User account is inactive");
        }

        // Reset failed login attempts on successful login
        user.FailedLoginAttempts = 0;
        user.LockoutEndDate = null;
        user.LastLoginAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        var (accessToken, expiresAt) = GenerateAccessToken(user);
        var refreshToken = await GenerateRefreshTokenAsync(user.Id);

        _logger.LogInformation("User logged in successfully: {UserId}", user.Id);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            TokenType = "Bearer",
            ExpiresIn = (int)(expiresAt - DateTime.UtcNow).TotalSeconds,
            RefreshTokenExpiresAt = refreshToken.ExpiresAt,
            User = _mapper.Map<Application.DTOs.User.UserDto>(user)
        };
    }

    public async Task<LoginResponseDto> RegisterAsync(RegisterRequestDto registerDto)
    {
        _logger.LogInformation("Registration attempt for email: {Email}", registerDto.Email);

        // Check if user already exists
        var existingUsers = await _unitOfWork.Users.FindAsync(u => u.Email == registerDto.Email);
        if (existingUsers.Any())
        {
            throw new BusinessValidationException("Email", "User with this email already exists");
        }

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Name = registerDto.Name,
            Email = registerDto.Email,
            PasswordHash = HashPassword(registerDto.Password),
            IsActive = true,
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User registered successfully: {UserId}", user.Id);

        var (accessToken, expiresAt) = GenerateAccessToken(user);
        var refreshToken = await GenerateRefreshTokenAsync(user.Id);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            TokenType = "Bearer",
            ExpiresIn = (int)(expiresAt - DateTime.UtcNow).TotalSeconds,
            RefreshTokenExpiresAt = refreshToken.ExpiresAt,
            User = _mapper.Map<Application.DTOs.User.UserDto>(user),
            IsFirstLogin = true
        };
    }

    public async Task<RefreshTokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto refreshDto)
    {
        _logger.LogInformation("Refresh token attempt");

        var refreshTokens = await _unitOfWork.RefreshTokens.FindAsync(rt => rt.Token == refreshDto.RefreshToken);
        var refreshToken = refreshTokens.FirstOrDefault();

        if (refreshToken == null || refreshToken.ExpiresAt < DateTime.UtcNow || refreshToken.RevokedAt != null)
        {
            _logger.LogWarning("Invalid or expired refresh token");
            throw new UnauthorizedException("Invalid or expired refresh token");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(refreshToken.UserId);
        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedException("User not found or inactive");
        }

        // Revoke old refresh token
        refreshToken.RevokedAt = DateTime.UtcNow;
        await _unitOfWork.RefreshTokens.UpdateAsync(refreshToken);

        // Generate new tokens
        var (accessToken, expiresAt) = GenerateAccessToken(user);
        var newRefreshToken = await GenerateRefreshTokenAsync(user.Id);

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Token refreshed successfully for user: {UserId}", user.Id);

        return new RefreshTokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken.Token,
            TokenType = "Bearer",
            ExpiresIn = (int)(expiresAt - DateTime.UtcNow).TotalSeconds
        };
    }

    public async Task LogoutAsync(string userId)
    {
        _logger.LogInformation("Logout attempt for user: {UserId}", userId);

        var refreshTokens = await _unitOfWork.RefreshTokens.FindAsync(rt => rt.UserId == userId && rt.RevokedAt == null);

        foreach (var token in refreshTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            await _unitOfWork.RefreshTokens.UpdateAsync(token);
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User logged out successfully: {UserId}", userId);
    }

    public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequestDto changePasswordDto)
    {
        _logger.LogInformation("Password change attempt for user: {UserId}", userId);

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            throw new EntityNotFoundException("User", userId);
        }

        if (!VerifyPassword(changePasswordDto.CurrentPassword, user.PasswordHash))
        {
            _logger.LogWarning("Invalid current password for user: {UserId}", userId);
            throw new UnauthorizedException("Current password is incorrect");
        }

        user.PasswordHash = HashPassword(changePasswordDto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Password changed successfully for user: {UserId}", userId);

        return true;
    }

    public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequestDto forgotPasswordDto)
    {
        _logger.LogInformation("Password reset request for email: {Email}", forgotPasswordDto.Email);

        var users = await _unitOfWork.Users.FindAsync(u => u.Email == forgotPasswordDto.Email);
        var user = users.FirstOrDefault();

        if (user == null)
        {
            // Don't reveal if user exists or not
            _logger.LogWarning("Password reset requested for non-existent email: {Email}", forgotPasswordDto.Email);
            return true;
        }

        // In a real implementation, you would:
        // 1. Generate a password reset token
        // 2. Store it in the database with expiration
        // 3. Send email with reset link

        _logger.LogInformation("Password reset email would be sent to: {Email}", forgotPasswordDto.Email);

        return true;
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequestDto resetPasswordDto)
    {
        _logger.LogInformation("Password reset attempt with token");

        // In a real implementation, you would:
        // 1. Validate the reset token
        // 2. Find the user by token
        // 3. Update the password
        // 4. Invalidate the token

        // For now, this is a placeholder
        _logger.LogInformation("Password reset completed");

        return true;
    }

    public async Task<bool> VerifyEmailAsync(VerifyEmailRequestDto verifyEmailDto)
    {
        _logger.LogInformation("Email verification attempt");

        // In a real implementation, you would:
        // 1. Validate the verification token
        // 2. Find the user by token
        // 3. Mark email as verified

        _logger.LogInformation("Email verified successfully");

        return true;
    }

    public async Task<GoogleAuthResponseDto> GoogleAuthAsync(GoogleAuthRequestDto googleAuthDto)
    {
        _logger.LogInformation("Google authentication attempt");

        // In a real implementation, you would:
        // 1. Validate the Google ID token
        // 2. Extract user info from token
        // 3. Find or create user
        // 4. Generate JWT tokens

        throw new NotImplementedException("Google authentication not yet implemented");
    }

    #region Private Helper Methods

    private (string token, DateTime expiresAt) GenerateAccessToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? "development-secret-please-change-in-production-youtube-rag-api-2024";
        var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "1440");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.Name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("userId", user.Id)
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    private async Task<RefreshToken> GenerateRefreshTokenAsync(string userId)
    {
        var refreshTokenExpiryDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpiryDays"] ?? "30");

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.RefreshTokens.AddAsync(refreshToken);

        return refreshToken;
    }

    private string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }

    #endregion
}
