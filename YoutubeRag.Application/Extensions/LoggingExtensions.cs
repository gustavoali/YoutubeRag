using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace YoutubeRag.Application.Extensions;

/// <summary>
/// Extension methods for enhanced structured logging capabilities
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Creates a logging scope with multiple properties for structured logging
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="properties">Key-value pairs to include in the logging scope</param>
    /// <returns>An IDisposable scope that should be used in a using statement</returns>
    public static IDisposable? BeginScopeWith(this ILogger logger, params (string key, object? value)[] properties)
    {
        if (!properties.Any())
        {
            return null;
        }

        var dictionary = properties.ToDictionary(p => p.key, p => p.value);
        return logger.BeginScope(dictionary);
    }

    /// <summary>
    /// Logs a timed operation with structured logging
    /// </summary>
    public static IDisposable? BeginTimedOperation(this ILogger logger, string operationName, params (string key, object? value)[] additionalProperties)
    {
        return new TimedLogOperation(logger, operationName, additionalProperties);
    }

    /// <summary>
    /// Logs entry into a method with parameters
    /// </summary>
    public static void LogMethodEntry(this ILogger logger, string methodName, object? parameters = null)
    {
        if (parameters != null)
        {
            logger.LogDebug("Entering {MethodName} with parameters: {@Parameters}", methodName, parameters);
        }
        else
        {
            logger.LogDebug("Entering {MethodName}", methodName);
        }
    }

    /// <summary>
    /// Logs exit from a method with optional result
    /// </summary>
    public static void LogMethodExit(this ILogger logger, string methodName, object? result = null, long? elapsedMs = null)
    {
        if (result != null && elapsedMs.HasValue)
        {
            logger.LogDebug("Exiting {MethodName} after {ElapsedMs}ms with result: {@Result}",
                methodName, elapsedMs.Value, result);
        }
        else if (elapsedMs.HasValue)
        {
            logger.LogDebug("Exiting {MethodName} after {ElapsedMs}ms", methodName, elapsedMs.Value);
        }
        else if (result != null)
        {
            logger.LogDebug("Exiting {MethodName} with result: {@Result}", methodName, result);
        }
        else
        {
            logger.LogDebug("Exiting {MethodName}", methodName);
        }
    }

    /// <summary>
    /// Logs a business event with structured data
    /// </summary>
    public static void LogBusinessEvent(this ILogger logger, string eventName, object? eventData = null, string? userId = null)
    {
        using (logger.BeginScopeWith(("EventName", eventName), ("UserId", userId)))
        {
            if (eventData != null)
            {
                logger.LogInformation("Business event {EventName} occurred with data: {@EventData}", eventName, eventData);
            }
            else
            {
                logger.LogInformation("Business event {EventName} occurred", eventName);
            }
        }
    }

    /// <summary>
    /// Logs a security-related event
    /// </summary>
    public static void LogSecurityEvent(this ILogger logger, string eventType, string userId, string resource, bool success, string? reason = null)
    {
        using (logger.BeginScopeWith(
            ("SecurityEventType", eventType),
            ("UserId", userId),
            ("Resource", resource),
            ("Success", success),
            ("Reason", reason)))
        {
            if (success)
            {
                logger.LogInformation("Security event {EventType} succeeded for user {UserId} accessing {Resource}",
                    eventType, userId, resource);
            }
            else
            {
                logger.LogWarning("Security event {EventType} failed for user {UserId} accessing {Resource}. Reason: {Reason}",
                    eventType, userId, resource, reason ?? "Unknown");
            }
        }
    }

    /// <summary>
    /// Logs database operation metrics
    /// </summary>
    public static void LogDatabaseOperation(this ILogger logger, string operation, string entity, long elapsedMs, int? recordCount = null)
    {
        using (logger.BeginScopeWith(
            ("DatabaseOperation", operation),
            ("Entity", entity),
            ("ElapsedMs", elapsedMs),
            ("RecordCount", recordCount)))
        {
            if (elapsedMs > 1000)
            {
                logger.LogWarning("Slow database operation {Operation} on {Entity} took {ElapsedMs}ms affecting {RecordCount} records",
                    operation, entity, elapsedMs, recordCount ?? 0);
            }
            else
            {
                logger.LogDebug("Database operation {Operation} on {Entity} completed in {ElapsedMs}ms affecting {RecordCount} records",
                    operation, entity, elapsedMs, recordCount ?? 0);
            }
        }
    }

    /// <summary>
    /// Logs external API call metrics
    /// </summary>
    public static void LogExternalApiCall(this ILogger logger, string service, string endpoint, int statusCode, long elapsedMs, string? error = null)
    {
        using (logger.BeginScopeWith(
            ("ExternalService", service),
            ("Endpoint", endpoint),
            ("StatusCode", statusCode),
            ("ElapsedMs", elapsedMs)))
        {
            if (statusCode >= 500)
            {
                logger.LogError("External API call to {Service} {Endpoint} failed with status {StatusCode} after {ElapsedMs}ms. Error: {Error}",
                    service, endpoint, statusCode, elapsedMs, error ?? "Unknown");
            }
            else if (statusCode >= 400)
            {
                logger.LogWarning("External API call to {Service} {Endpoint} returned client error {StatusCode} after {ElapsedMs}ms",
                    service, endpoint, statusCode, elapsedMs);
            }
            else if (elapsedMs > 3000)
            {
                logger.LogWarning("Slow external API call to {Service} {Endpoint} took {ElapsedMs}ms",
                    service, endpoint, elapsedMs);
            }
            else
            {
                logger.LogInformation("External API call to {Service} {Endpoint} succeeded with status {StatusCode} in {ElapsedMs}ms",
                    service, endpoint, statusCode, elapsedMs);
            }
        }
    }

    /// <summary>
    /// Helper class for timing operations
    /// </summary>
    private class TimedLogOperation : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;
        private readonly IDisposable? _scope;

        public TimedLogOperation(ILogger logger, string operationName, (string key, object? value)[] additionalProperties)
        {
            _logger = logger;
            _operationName = operationName;
            _stopwatch = Stopwatch.StartNew();

            var properties = additionalProperties.Append(("Operation", operationName)).ToArray();
            _scope = logger.BeginScopeWith(properties);

            logger.LogDebug("Starting operation {OperationName}", operationName);
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _logger.LogDebug("Completed operation {OperationName} in {ElapsedMs}ms",
                _operationName, _stopwatch.ElapsedMilliseconds);
            _scope?.Dispose();
        }
    }
}
