using System.Text.Json;
using Microsoft.Playwright;

namespace YoutubeRag.Tests.E2E.PageObjects;

/// <summary>
/// Page Object for Authentication API endpoints
/// </summary>
public class AuthApi : ApiClient
{
    public AuthApi(IAPIRequestContext requestContext, string baseUrl)
        : base(requestContext, baseUrl)
    {
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    public async Task<IAPIResponse> RegisterAsync(string email, string password, string name)
    {
        var data = new
        {
            email,
            password,
            name
        };

        return await PostAsync("/auth/register", data);
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    public async Task<IAPIResponse> LoginAsync(string email, string password)
    {
        var data = new
        {
            email,
            password
        };

        return await PostAsync("/auth/login", data);
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    public async Task<IAPIResponse> GetCurrentUserAsync()
    {
        return await GetAsync("/auth/me");
    }

    /// <summary>
    /// Logout current user
    /// </summary>
    public async Task<IAPIResponse> LogoutAsync()
    {
        return await PostAsync("/auth/logout");
    }

    /// <summary>
    /// Refresh access token
    /// </summary>
    public async Task<IAPIResponse> RefreshTokenAsync(string refreshToken)
    {
        var data = new Dictionary<string, string>
        {
            ["refresh_token"] = refreshToken
        };

        return await PostAsync("/auth/refresh", data);
    }

    /// <summary>
    /// Extract access token from login/register response
    /// </summary>
    public async Task<string?> ExtractAccessTokenAsync(IAPIResponse response)
    {
        var responseBody = await response.TextAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return tokenResponse?.AccessToken;
    }
}

public class TokenResponse
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? TokenType { get; set; }
    public int ExpiresIn { get; set; }
}
