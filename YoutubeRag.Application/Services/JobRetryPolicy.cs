using Microsoft.Extensions.Logging;
using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Application.Services;

/// <summary>
/// Defines retry policies for different types of job failures
/// </summary>
public class JobRetryPolicy
{
    /// <summary>
    /// Category of failure this policy applies to
    /// </summary>
    public FailureCategory Category { get; set; }

    /// <summary>
    /// Maximum number of retry attempts for this failure category
    /// </summary>
    public int MaxRetries { get; set; }

    /// <summary>
    /// Initial delay before first retry
    /// </summary>
    public TimeSpan InitialDelay { get; set; }

    /// <summary>
    /// Whether to use exponential backoff (doubles delay each retry)
    /// </summary>
    public bool UseExponentialBackoff { get; set; }

    /// <summary>
    /// Whether this failure should go directly to Dead Letter Queue without retry
    /// </summary>
    public bool SendToDeadLetterQueue { get; set; }

    /// <summary>
    /// Description of the policy for logging purposes
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Calculates the next retry delay based on current retry count
    /// </summary>
    /// <param name="retryCount">Current retry attempt number (0-based)</param>
    /// <returns>TimeSpan until next retry</returns>
    public TimeSpan GetNextRetryDelay(int retryCount)
    {
        if (UseExponentialBackoff)
        {
            // Exponential backoff: InitialDelay * 2^retryCount
            var multiplier = Math.Pow(2, retryCount);
            return TimeSpan.FromSeconds(InitialDelay.TotalSeconds * multiplier);
        }
        else
        {
            // Linear backoff: InitialDelay * (retryCount + 1)
            return TimeSpan.FromSeconds(InitialDelay.TotalSeconds * (retryCount + 1));
        }
    }

    /// <summary>
    /// Gets the appropriate retry policy for a given exception
    /// </summary>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="logger">Logger for diagnostic information</param>
    /// <returns>The retry policy to apply</returns>
    public static JobRetryPolicy GetPolicy(Exception exception, ILogger? logger = null)
    {
        var category = ClassifyException(exception, logger);
        return GetPolicyForCategory(category);
    }

    /// <summary>
    /// Classifies an exception into a failure category
    /// </summary>
    /// <param name="exception">The exception to classify</param>
    /// <param name="logger">Logger for diagnostic information</param>
    /// <returns>The appropriate failure category</returns>
    public static FailureCategory ClassifyException(Exception exception, ILogger? logger = null)
    {
        var exceptionType = exception.GetType().Name;
        var exceptionMessage = exception.Message?.ToLowerInvariant() ?? string.Empty;

        // Check message patterns first before exception types
        // This allows specific error messages to override generic exception type classification

        // Resource not available - retry with linear backoff
        // Check this first because messages like "Model downloading" should take precedence
        if (IsResourceNotAvailable(exception, exceptionMessage))
        {
            logger?.LogInformation("Classified exception as ResourceNotAvailable: {ExceptionType} - {Message}",
                exceptionType, exception.Message);
            return FailureCategory.ResourceNotAvailable;
        }

        // Transient network errors - retry with exponential backoff
        if (IsTransientNetworkError(exception, exceptionMessage))
        {
            logger?.LogInformation("Classified exception as TransientNetworkError: {ExceptionType} - {Message}",
                exceptionType, exception.Message);
            return FailureCategory.TransientNetworkError;
        }

        // Permanent errors - no retry
        if (IsPermamentError(exception, exceptionMessage))
        {
            logger?.LogInformation("Classified exception as PermanentError: {ExceptionType} - {Message}",
                exceptionType, exception.Message);
            return FailureCategory.PermanentError;
        }

        // Unknown error - cautious retry
        logger?.LogWarning("Classified exception as UnknownError: {ExceptionType} - {Message}",
            exceptionType, exception.Message);
        return FailureCategory.UnknownError;
    }

    /// <summary>
    /// Gets the retry policy for a specific failure category
    /// </summary>
    /// <param name="category">The failure category</param>
    /// <returns>The retry policy configuration</returns>
    public static JobRetryPolicy GetPolicyForCategory(FailureCategory category)
    {
        return category switch
        {
            FailureCategory.TransientNetworkError => new JobRetryPolicy
            {
                Category = FailureCategory.TransientNetworkError,
                MaxRetries = 5,
                InitialDelay = TimeSpan.FromSeconds(10),
                UseExponentialBackoff = true,
                SendToDeadLetterQueue = false,
                Description = "Transient network errors: max 5 retries with exponential backoff (10s, 20s, 40s, 80s, 160s)"
            },

            FailureCategory.ResourceNotAvailable => new JobRetryPolicy
            {
                Category = FailureCategory.ResourceNotAvailable,
                MaxRetries = 3,
                InitialDelay = TimeSpan.FromMinutes(2),
                UseExponentialBackoff = false,
                SendToDeadLetterQueue = false,
                Description = "Resource unavailable: max 3 retries with linear backoff (2m, 4m, 6m)"
            },

            FailureCategory.PermanentError => new JobRetryPolicy
            {
                Category = FailureCategory.PermanentError,
                MaxRetries = 0,
                InitialDelay = TimeSpan.Zero,
                UseExponentialBackoff = false,
                SendToDeadLetterQueue = true,
                Description = "Permanent error: no retries, send directly to Dead Letter Queue"
            },

            FailureCategory.UnknownError => new JobRetryPolicy
            {
                Category = FailureCategory.UnknownError,
                MaxRetries = 2,
                InitialDelay = TimeSpan.FromSeconds(30),
                UseExponentialBackoff = true,
                SendToDeadLetterQueue = false,
                Description = "Unknown error: max 2 cautious retries with exponential backoff (30s, 60s)"
            },

            _ => throw new ArgumentException($"Unknown failure category: {category}", nameof(category))
        };
    }

    #region Exception Classification Helpers

    private static bool IsPermamentError(Exception exception, string message)
    {
        // Check exception types that indicate permanent failures
        // Note: InvalidOperationException is NOT included here because it can be transient
        // (e.g., "Model downloading - please wait", "Connection timeout")
        // These are classified by message patterns in IsResourceNotAvailable and IsTransientNetworkError
        if (exception is ArgumentException ||
            exception is ArgumentNullException ||
            exception is FormatException)
        {
            return true;
        }

        // Check message patterns for permanent errors
        var permanentPatterns = new[]
        {
            "video not found",
            "video deleted",
            "video unavailable",
            "invalid format",
            "invalid video",
            "access denied",
            "forbidden",
            "unauthorized",
            "not found",
            "does not exist",
            "private video",
            "region blocked",
            "copyright",
            "account terminated"
        };

        return permanentPatterns.Any(pattern => message.Contains(pattern));
    }

    private static bool IsTransientNetworkError(Exception exception, string message)
    {
        // Check exception types that indicate transient network failures
        if (exception is HttpRequestException ||
            exception is TimeoutException ||
            exception is TaskCanceledException)
        {
            return true;
        }

        // Check message patterns for transient network errors
        var transientPatterns = new[]
        {
            "timeout",
            "timed out",
            "connection reset",
            "connection refused",
            "connection failed",
            "network error",
            "socket error",
            "http 5",
            "service unavailable",
            "gateway timeout",
            "bad gateway",
            "too many requests",
            "rate limit",
            "throttle"
        };

        return transientPatterns.Any(pattern => message.Contains(pattern));
    }

    private static bool IsResourceNotAvailable(Exception exception, string message)
    {
        // Check message patterns for resource unavailability
        var resourcePatterns = new[]
        {
            "disk full",
            "out of disk space",
            "no space left",
            "model downloading",
            "model not ready",
            "downloading model",
            "insufficient storage",
            "insufficient memory",
            "out of memory",
            "resource busy",
            "resource locked",
            "file in use"
        };

        return resourcePatterns.Any(pattern => message.Contains(pattern));
    }

    #endregion
}
