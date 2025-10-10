using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace YoutubeRag.Api.Middleware;

/// <summary>
/// Middleware for handling validation exceptions and returning standardized error responses
/// </summary>
public class ValidationExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ValidationExceptionMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationExceptionMiddleware"/> class
    /// </summary>
    public ValidationExceptionMiddleware(RequestDelegate next, ILogger<ValidationExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Processes the HTTP request and handles validation exceptions
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation exception occurred");
            await HandleValidationExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Handles validation exceptions and returns a standardized error response
    /// </summary>
    private async Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";

        var errors = exception.Errors
            .GroupBy(x => x.PropertyName)
            .ToDictionary(
                g => ToCamelCase(g.Key),
                g => g.Select(x => x.ErrorMessage).ToArray()
            );

        var problemDetails = new ValidationProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Detail = "Please refer to the errors property for additional details.",
            Instance = context.Request.Path
        };

        // Add errors to the problem details
        foreach (var error in errors)
        {
            problemDetails.Errors.Add(error.Key, error.Value);
        }

        // Add trace ID for debugging
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(problemDetails, jsonOptions);
        await context.Response.WriteAsync(json);
    }

    /// <summary>
    /// Converts a string to camel case
    /// </summary>
    private string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str)) return str;
        if (str.Length == 1) return str.ToLowerInvariant();

        // Handle nested property names (e.g., "Address.Street" -> "address.street")
        var parts = str.Split('.');
        return string.Join(".", parts.Select(part =>
        {
            if (string.IsNullOrEmpty(part)) return part;
            return char.ToLowerInvariant(part[0]) + part.Substring(1);
        }));
    }
}