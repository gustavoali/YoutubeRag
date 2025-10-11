using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace YoutubeRag.Api.Middleware;

/// <summary>
/// Global exception handler middleware for handling all unhandled exceptions
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalExceptionHandlerMiddleware"/> class
    /// </summary>
    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Processes the HTTP request and handles exceptions
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Handles exceptions and returns appropriate error responses
    /// </summary>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Log the exception
        _logger.LogError(exception, "An unhandled exception occurred");

        // Set response content type
        context.Response.ContentType = "application/problem+json";

        // Determine status code and problem details based on exception type
        ProblemDetails problemDetails;

        switch (exception)
        {
            case ValidationException validationException:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                problemDetails = CreateValidationProblemDetails(context, validationException);
                break;

            case ArgumentNullException argumentNullException:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                problemDetails = CreateProblemDetails(
                    context,
                    StatusCodes.Status400BadRequest,
                    "Bad Request",
                    $"Required parameter is missing: {argumentNullException.ParamName}");
                break;

            case ArgumentException argumentException:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                problemDetails = CreateProblemDetails(
                    context,
                    StatusCodes.Status400BadRequest,
                    "Bad Request",
                    argumentException.Message);
                break;

            case UnauthorizedAccessException:
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                problemDetails = CreateProblemDetails(
                    context,
                    StatusCodes.Status401Unauthorized,
                    "Unauthorized",
                    "You are not authorized to access this resource");
                break;

            case KeyNotFoundException:
            case FileNotFoundException:
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                problemDetails = CreateProblemDetails(
                    context,
                    StatusCodes.Status404NotFound,
                    "Not Found",
                    "The requested resource was not found");
                break;

            case TimeoutException:
                context.Response.StatusCode = StatusCodes.Status408RequestTimeout;
                problemDetails = CreateProblemDetails(
                    context,
                    StatusCodes.Status408RequestTimeout,
                    "Request Timeout",
                    "The request took too long to process");
                break;

            case NotImplementedException:
                context.Response.StatusCode = StatusCodes.Status501NotImplemented;
                problemDetails = CreateProblemDetails(
                    context,
                    StatusCodes.Status501NotImplemented,
                    "Not Implemented",
                    "This feature is not yet implemented");
                break;

            case InvalidOperationException invalidOperationException:
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                problemDetails = CreateProblemDetails(
                    context,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    invalidOperationException.Message);
                break;

            default:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                problemDetails = CreateProblemDetails(
                    context,
                    StatusCodes.Status500InternalServerError,
                    "Internal Server Error",
                    _environment.IsDevelopment() ? exception.Message : "An error occurred while processing your request");
                break;
        }

        // Add exception details in development
        if (_environment.IsDevelopment() && !(exception is ValidationException))
        {
            problemDetails.Extensions["exception"] = new
            {
                type = exception.GetType().Name,
                message = exception.Message,
                stackTrace = exception.StackTrace
            };
        }

        // Serialize and write response
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(problemDetails, jsonOptions);
        await context.Response.WriteAsync(json);
    }

    /// <summary>
    /// Creates problem details for general exceptions
    /// </summary>
    private ProblemDetails CreateProblemDetails(HttpContext context, int statusCode, string title, string detail)
    {
        return new ProblemDetails
        {
            Type = GetProblemTypeUri(statusCode),
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = context.Request.Path,
            Extensions =
            {
                ["traceId"] = context.TraceIdentifier,
                ["timestamp"] = DateTimeOffset.UtcNow
            }
        };
    }

    /// <summary>
    /// Creates validation problem details for validation exceptions
    /// </summary>
    private ValidationProblemDetails CreateValidationProblemDetails(HttpContext context, ValidationException exception)
    {
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

        foreach (var error in errors)
        {
            problemDetails.Errors.Add(error.Key, error.Value);
        }

        problemDetails.Extensions["traceId"] = context.TraceIdentifier;
        problemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;

        return problemDetails;
    }

    /// <summary>
    /// Gets the problem type URI based on status code
    /// </summary>
    private string GetProblemTypeUri(int statusCode)
    {
        return statusCode switch
        {
            400 => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            401 => "https://tools.ietf.org/html/rfc7235#section-3.1",
            403 => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            404 => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            408 => "https://tools.ietf.org/html/rfc7231#section-6.5.7",
            409 => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            500 => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            501 => "https://tools.ietf.org/html/rfc7231#section-6.6.2",
            _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };
    }

    /// <summary>
    /// Converts a string to camel case
    /// </summary>
    private string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }

        if (str.Length == 1)
        {
            return str.ToLowerInvariant();
        }

        var parts = str.Split('.');
        return string.Join(".", parts.Select(part =>
        {
            if (string.IsNullOrEmpty(part))
            {
                return part;
            }

            return char.ToLowerInvariant(part[0]) + part.Substring(1);
        }));
    }
}
