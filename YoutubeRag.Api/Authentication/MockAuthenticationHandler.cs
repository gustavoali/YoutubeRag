using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace YoutubeRag.Api.Authentication;

public class MockAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IWebHostEnvironment _environment;

    public MockAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IWebHostEnvironment environment)
        : base(options, logger, encoder)
    {
        _environment = environment;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // CRITICAL: Prevent mock authentication in production
        if (_environment.IsProduction())
        {
            Logger.LogCritical("SECURITY VIOLATION: MockAuthenticationHandler is being used in Production environment!");
            throw new InvalidOperationException(
                "Mock authentication cannot be used in production. " +
                "Ensure EnableAuth is set to true in production configuration.");
        }

        Logger.LogWarning("MockAuthenticationHandler is active - This should only be used in Development/Test environments");
        // Check if the Authorization header exists
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            Logger.LogInformation("Mock authentication failed - No Authorization header");
            return Task.FromResult(AuthenticateResult.Fail("No Authorization header"));
        }

        var authHeader = Request.Headers["Authorization"].ToString();

        // Check if the header starts with "Bearer "
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            Logger.LogInformation("Mock authentication failed - Invalid Authorization header format");
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header format"));
        }

        // Extract token
        var token = authHeader.Substring("Bearer ".Length).Trim();

        // Check if token is empty or invalid (for testing purposes, we'll accept any non-empty token)
        if (string.IsNullOrWhiteSpace(token))
        {
            Logger.LogInformation("Mock authentication failed - Empty token");
            return Task.FromResult(AuthenticateResult.Fail("Empty token"));
        }

        // For testing purposes, check if token is explicitly "invalid" to simulate invalid token
        if (token.Equals("invalid", StringComparison.OrdinalIgnoreCase))
        {
            Logger.LogInformation("Mock authentication failed - Invalid token");
            return Task.FromResult(AuthenticateResult.Fail("Invalid token"));
        }

        // Try to parse the JWT token to extract actual claims
        var claims = new List<Claim>();
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(token))
            {
                var jwtToken = handler.ReadJwtToken(token);

                // Extract claims from the JWT token
                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? "test-user-id";
                var userName = jwtToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? "test-user";
                var userEmail = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? "test@example.com";

                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
                claims.Add(new Claim(ClaimTypes.Name, userName));
                claims.Add(new Claim(ClaimTypes.Email, userEmail));
                claims.Add(new Claim("scope", "api.access"));
                claims.Add(new Claim("userId", userId));

                Logger.LogInformation($"Mock authentication successful with JWT - UserId: {userId}");
            }
            else
            {
                // Fall back to default test user if not a valid JWT
                claims.Add(new Claim(ClaimTypes.NameIdentifier, "test-user-id"));
                claims.Add(new Claim(ClaimTypes.Name, "test-user"));
                claims.Add(new Claim(ClaimTypes.Email, "test@example.com"));
                claims.Add(new Claim("scope", "api.access"));

                Logger.LogInformation("Mock authentication successful with default test user");
            }
        }
        catch
        {
            // Fall back to default test user if JWT parsing fails
            claims.Add(new Claim(ClaimTypes.NameIdentifier, "test-user-id"));
            claims.Add(new Claim(ClaimTypes.Name, "test-user"));
            claims.Add(new Claim(ClaimTypes.Email, "test@example.com"));
            claims.Add(new Claim("scope", "api.access"));

            Logger.LogInformation("Mock authentication successful with default test user (JWT parse failed)");
        }

        var identity = new ClaimsIdentity(claims, "Mock");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Mock");

        Logger.LogInformation($"Mock authentication successful - Token: {token.Substring(0, Math.Min(10, token.Length))}...");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
