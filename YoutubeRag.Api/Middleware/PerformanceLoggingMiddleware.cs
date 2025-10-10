using System.Diagnostics;
using Serilog.Context;

namespace YoutubeRag.Api.Middleware;

/// <summary>
/// Middleware for performance monitoring and logging slow requests
/// </summary>
public class PerformanceLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceLoggingMiddleware> _logger;
    private readonly IConfiguration _configuration;
    private const int DefaultSlowRequestThresholdMs = 1000;

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceLoggingMiddleware"/> class
    /// </summary>
    public PerformanceLoggingMiddleware(
        RequestDelegate next,
        ILogger<PerformanceLoggingMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Processes the HTTP request and logs performance metrics
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // Get configurable threshold for slow requests
        var slowRequestThresholdMs = _configuration.GetValue<int>("Performance:SlowRequestThresholdMs", DefaultSlowRequestThresholdMs);

        var stopwatch = Stopwatch.StartNew();
        var path = context.Request.Path;
        var method = context.Request.Method;
        var userAgent = context.Request.Headers["User-Agent"].ToString();
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // Add performance tracking to log context
        using (LogContext.PushProperty("RequestPath", path))
        using (LogContext.PushProperty("RequestMethod", method))
        using (LogContext.PushProperty("UserAgent", userAgent))
        using (LogContext.PushProperty("RemoteIP", remoteIp))
        {
            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                var elapsedMs = stopwatch.ElapsedMilliseconds;

                // Always log elapsed time as structured data
                using (LogContext.PushProperty("ElapsedMs", elapsedMs))
                using (LogContext.PushProperty("StatusCode", context.Response.StatusCode))
                {
                    // Log based on performance characteristics
                    if (elapsedMs > slowRequestThresholdMs * 2)
                    {
                        // Critical performance issue
                        _logger.LogError(
                            "Critical performance issue: {Method} {Path} took {ElapsedMs}ms (Status: {StatusCode})",
                            method, path, elapsedMs, context.Response.StatusCode);
                    }
                    else if (elapsedMs > slowRequestThresholdMs)
                    {
                        // Slow request warning
                        _logger.LogWarning(
                            "Slow request detected: {Method} {Path} took {ElapsedMs}ms (Status: {StatusCode})",
                            method, path, elapsedMs, context.Response.StatusCode);
                    }
                    else if (context.Response.StatusCode >= 500)
                    {
                        // Server error with timing
                        _logger.LogError(
                            "Server error: {Method} {Path} completed in {ElapsedMs}ms with status {StatusCode}",
                            method, path, elapsedMs, context.Response.StatusCode);
                    }
                    else if (context.Response.StatusCode >= 400)
                    {
                        // Client error with timing
                        _logger.LogWarning(
                            "Client error: {Method} {Path} completed in {ElapsedMs}ms with status {StatusCode}",
                            method, path, elapsedMs, context.Response.StatusCode);
                    }
                    else
                    {
                        // Normal request - log at debug level
                        _logger.LogDebug(
                            "Request completed: {Method} {Path} in {ElapsedMs}ms (Status: {StatusCode})",
                            method, path, elapsedMs, context.Response.StatusCode);
                    }

                    // Log additional performance metrics for API endpoints
                    if (path.StartsWithSegments("/api"))
                    {
                        LogApiMetrics(context, elapsedMs);
                    }
                }
            }
        }
    }

    private void LogApiMetrics(HttpContext context, long elapsedMs)
    {
        var metrics = new
        {
            Endpoint = context.GetEndpoint()?.DisplayName ?? "Unknown",
            Controller = context.GetRouteValue("controller")?.ToString(),
            Action = context.GetRouteValue("action")?.ToString(),
            ResponseSize = context.Response.ContentLength,
            ElapsedMs = elapsedMs,
            StatusCode = context.Response.StatusCode,
            User = context.User?.Identity?.Name ?? "Anonymous"
        };

        _logger.LogInformation("API Metrics: {@Metrics}", metrics);
    }
}

/// <summary>
/// Extension methods for adding Performance Logging middleware to the application pipeline
/// </summary>
public static class PerformanceLoggingMiddlewareExtensions
{
    /// <summary>
    /// Adds the performance logging middleware to the application pipeline
    /// </summary>
    public static IApplicationBuilder UsePerformanceLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PerformanceLoggingMiddleware>();
    }
}