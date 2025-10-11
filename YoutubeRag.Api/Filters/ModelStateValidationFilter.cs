using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace YoutubeRag.Api.Filters;

/// <summary>
/// Action filter that validates model state and returns standardized validation errors
/// </summary>
public class ModelStateValidationFilter : IActionFilter
{
    /// <summary>
    /// Called before the action executes
    /// </summary>
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => ToCamelCase(kvp.Key),
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            var problemDetails = new ValidationProblemDetails(context.ModelState)
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Please refer to the errors property for additional details.",
                Instance = context.HttpContext.Request.Path
            };

            problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

            context.Result = new BadRequestObjectResult(problemDetails);
        }
    }

    /// <summary>
    /// Called after the action executes
    /// </summary>
    public void OnActionExecuted(ActionExecutedContext context)
    {
        // No implementation needed
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

        // Handle nested property names (e.g., "Address.Street" -> "address.street")
        var parts = str.Split('.');
        return string.Join(".", parts.Select(part =>
        {
            if (string.IsNullOrEmpty(part))
            {
                return part;
            }

            // Handle array indexers (e.g., "Items[0]" -> "items[0]")
            var indexerStart = part.IndexOf('[');
            if (indexerStart > 0)
            {
                var propertyName = part.Substring(0, indexerStart);
                var indexer = part.Substring(indexerStart);
                return char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1) + indexer;
            }

            return char.ToLowerInvariant(part[0]) + part.Substring(1);
        }));
    }
}
