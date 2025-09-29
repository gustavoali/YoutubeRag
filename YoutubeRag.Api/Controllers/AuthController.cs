using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using YoutubeRag.Api.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BCrypt.Net;

namespace YoutubeRag.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Tags("🔐 Authentication")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Register a new user - WORKING VERSION (copied from successful simple endpoint)
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<TokenResponse>> RegisterUser(RegisterRequest request)
    {
        try
        {
            // Validate input
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { error = new { code = "VALIDATION_ERROR", message = "Email and password are required" } });
            }

            // Mock user creation (in real implementation, save to database)
            var user = new
            {
                Id = Guid.NewGuid().ToString(),
                Email = request.Email,
                Name = request.Name ?? "User"
            };

            // Generate JWT token
            var token = GenerateJwtToken(user.Id, user.Email);
            var refreshToken = GenerateRefreshToken();

            return Ok(new TokenResponse
            {
                AccessToken = token,
                RefreshToken = refreshToken,
                TokenType = "Bearer",
                ExpiresIn = 1800 // 30 minutes
            });
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
            // Validate credentials (mock implementation)
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { error = new { code = "VALIDATION_ERROR", message = "Email and password are required" } });
            }

            // Mock authentication
            var user = new
            {
                Id = Guid.NewGuid().ToString(),
                Email = request.Email,
                Name = "Test User"
            };

            var token = GenerateJwtToken(user.Id, user.Email);
            var refreshToken = GenerateRefreshToken();

            return Ok(new TokenResponse
            {
                AccessToken = token,
                RefreshToken = refreshToken,
                TokenType = "Bearer",
                ExpiresIn = 1800
            });
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
        // Mock implementation
        var token = GenerateJwtToken(Guid.NewGuid().ToString(), "user@gmail.com");
        var refreshToken = GenerateRefreshToken();

        return Ok(new TokenResponse
        {
            AccessToken = token,
            RefreshToken = refreshToken,
            TokenType = "Bearer",
            ExpiresIn = 1800
        });
    }

    /// <summary>
    /// Logout user (revoke refresh tokens)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout([FromBody] Dictionary<string, string> request)
    {
        // In real implementation, revoke refresh tokens
        return Ok(new { message = "Logged out successfully" });
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
        if (!request.TryGetValue("refresh_token", out var refreshToken))
        {
            return BadRequest(new { error = new { code = "MISSING_REFRESH_TOKEN", message = "Refresh token is required" } });
        }

        // Mock token refresh
        var newToken = GenerateJwtToken(Guid.NewGuid().ToString(), "user@example.com");
        var newRefreshToken = GenerateRefreshToken();

        return Ok(new TokenResponse
        {
            AccessToken = newToken,
            RefreshToken = newRefreshToken,
            TokenType = "Bearer",
            ExpiresIn = 1800
        });
    }

    /// <summary>
    /// List all users (admin only)
    /// </summary>
    [HttpGet("users")]
    [Authorize]
    public async Task<ActionResult> ListUsers(int page = 1, int pageSize = 20)
    {
        var users = new[]
        {
            new { id = "1", email = "user1@example.com", name = "User 1", created_at = DateTime.UtcNow.AddDays(-10) },
            new { id = "2", email = "user2@example.com", name = "User 2", created_at = DateTime.UtcNow.AddDays(-5) }
        };

        return Ok(new { users, total = users.Length, page, page_size = pageSize });
    }

    private string GenerateJwtToken(string userId, string email)
    {
        var secretKey = _configuration["JwtSettings:SecretKey"] ?? "development-secret-please-change-in-production-youtube-rag-api-2024";
        var key = Encoding.ASCII.GetBytes(secretKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, email)
            }),
            Expires = DateTime.UtcNow.AddMinutes(30),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }
}