using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace YoutubeRag.Api.Authentication;

public class MockAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public MockAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Create a mock user for development/testing
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "anonymous-user"),
            new Claim(ClaimTypes.Name, "anonymous-user"),
            new Claim(ClaimTypes.Email, "anonymous@localhost"),
            new Claim("scope", "api.access")
        };

        var identity = new ClaimsIdentity(claims, "Mock");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Mock");

        Logger.LogInformation("Mock authentication successful for development/testing");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}