using Hangfire.Dashboard;

namespace YoutubeRag.Api.Filters;

/// <summary>
/// Custom authorization filter for Hangfire Dashboard
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        var env = httpContext.RequestServices
            .GetRequiredService<IWebHostEnvironment>();

        // Allow access in Development or Local environments
        if (env.IsDevelopment() || env.EnvironmentName.Equals("Local", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // In production, require authentication and admin role
        return httpContext.User.Identity?.IsAuthenticated == true &&
               httpContext.User.IsInRole("Admin");
    }
}
