using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YoutubeRag.Api.Models;
using YoutubeRag.Application.DTOs.Auth;
using YoutubeRag.Application.Exceptions;
using YoutubeRag.Application.Interfaces.Services;

namespace YoutubeRag.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Tags("🔐 Authentication")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<TokenResponse>> RegisterUser(RegisterRequest request)
    {
        try
        {
            var registerDto = new RegisterRequestDto
            {
                Name = request.Name ?? "User",
                Email = request.Email,
                Password = request.Password
            };

            var result = await _authService.RegisterAsync(registerDto);

            return Ok(new TokenResponse
            {
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
                TokenType = result.TokenType,
                ExpiresIn = result.ExpiresIn
            });
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_ERROR", message = ex.Message, errors = ex.Errors } });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR", message = ex.Message } });
        }
    }

    /// <summary>
    /// Traditional email/password login
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<TokenResponse>> LoginWithPassword(LoginRequest request)
    {
        try
        {
            var loginDto = new LoginRequestDto
            {
                Email = request.Email,
                Password = request.Password
            };

            var result = await _authService.LoginAsync(loginDto);

            return Ok(new TokenResponse
            {
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
                TokenType = result.TokenType,
                ExpiresIn = result.ExpiresIn
            });
        }
        catch (UnauthorizedException ex)
        {
            return Unauthorized(new { error = new { code = "UNAUTHORIZED", message = ex.Message } });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR", message = ex.Message } });
        }
    }

    /// <summary>
    /// Initiate Google OAuth flow
    /// </summary>
    [HttpGet("google")]
    public ActionResult GoogleOAuthLogin()
    {
        var redirectUrl = "https://accounts.google.com/o/oauth2/auth";
        return Ok(new { redirect_url = redirectUrl, message = "Redirect to Google OAuth" });
    }

    /// <summary>
    /// Handle Google OAuth callback
    /// </summary>
    [HttpGet("google/callback")]
    public async Task<ActionResult> GoogleOAuthCallback(string? code, string? error, string? state)
    {
        if (!string.IsNullOrEmpty(error))
        {
            return BadRequest(new { error = new { code = "OAUTH_ERROR", message = error } });
        }

        return Ok(new { message = "Google OAuth callback received", code });
    }

    /// <summary>
    /// Exchange Google OAuth code for tokens (for frontend AJAX calls)
    /// </summary>
    [HttpPost("google/exchange")]
    public async Task<ActionResult<TokenResponse>> ExchangeGoogleCode(GoogleCallbackRequest request)
    {
        try
        {
            var googleAuthDto = new GoogleAuthRequestDto
            {
                GoogleToken = request.Code ?? string.Empty
            };

            var result = await _authService.GoogleAuthAsync(googleAuthDto);

            return Ok(new TokenResponse
            {
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
                TokenType = "Bearer",
                ExpiresIn = 1800
            });
        }
        catch (NotImplementedException)
        {
            return StatusCode(501, new { error = new { code = "NOT_IMPLEMENTED", message = "Google authentication not yet implemented" } });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR", message = ex.Message } });
        }
    }

    /// <summary>
    /// Logout user (revoke refresh tokens)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = new { code = "UNAUTHORIZED", message = "User not authenticated" } });
            }

            await _authService.LogoutAsync(userId);
            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR", message = ex.Message } });
        }
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserProfile>> GetCurrentUserInfo()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;

        return Ok(new UserProfile
        {
            Id = userId ?? "",
            Email = email ?? "",
            Name = "Test User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        });
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponse>> RefreshAccessToken([FromBody] Dictionary<string, string> request)
    {
        try
        {
            if (!request.TryGetValue("refresh_token", out var refreshToken))
            {
                return BadRequest(new { error = new { code = "MISSING_REFRESH_TOKEN", message = "Refresh token is required" } });
            }

            var refreshDto = new RefreshTokenRequestDto
            {
                RefreshToken = refreshToken
            };

            var result = await _authService.RefreshTokenAsync(refreshDto);

            return Ok(new TokenResponse
            {
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
                TokenType = result.TokenType,
                ExpiresIn = result.ExpiresIn
            });
        }
        catch (UnauthorizedException ex)
        {
            return Unauthorized(new { error = new { code = "UNAUTHORIZED", message = ex.Message } });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR", message = ex.Message } });
        }
    }
}
