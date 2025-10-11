using Serilog.Context;

namespace YoutubeRag.Api.Middleware;

/// <summary>
/// Middleware for adding and tracking correlation IDs across requests for better distributed tracing
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private const string CorrelationIdHeader = "X-Correlation-Id";

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationIdMiddleware"/> class
    /// </summary>
    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Processes the HTTP request and adds correlation ID to the context
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // Try to get correlation ID from request header, or generate a new one
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        // Store correlation ID in HttpContext items for access throughout the request
        context.Items["CorrelationId"] = correlationId;

        // Add correlation ID to response headers for client tracking
        context.Response.Headers.Append(CorrelationIdHeader, correlationId);

        // Push correlation ID to Serilog context for all logs in this request
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            _logger.LogInformation("Processing request with CorrelationId {CorrelationId} for {Method} {Path}",
                correlationId, context.Request.Method, context.Request.Path);

            await _next(context);

            _logger.LogInformation("Completed request with CorrelationId {CorrelationId} - Status {StatusCode}",
                correlationId, context.Response.StatusCode);
        }
    }
}

/// <summary>
/// Extension methods for adding CorrelationId middleware to the application pipeline
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    /// <summary>
    /// Adds the correlation ID middleware to the application pipeline
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
}
