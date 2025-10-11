using System.Net;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace YoutubeRag.Infrastructure.Resilience;

/// <summary>
/// Polly resilience policies for external service calls
/// Implements retry logic with exponential backoff for network errors and rate limiting
/// </summary>
public static class PollyPolicies
{
    /// <summary>
    /// Creates a retry policy for YouTube API calls with exponential backoff
    /// Handles network errors, timeouts, and rate limiting (HTTP 429)
    /// </summary>
    /// <param name="logger">Logger for retry attempts</param>
    /// <param name="maxRetries">Maximum number of retry attempts (default: 3)</param>
    /// <returns>Async retry policy</returns>
    public static AsyncRetryPolicy<HttpResponseMessage> CreateYouTubeRetryPolicy(ILogger logger, int maxRetries = 3)
    {
        return Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutException>()
            .OrResult<HttpResponseMessage>(r =>
                r.StatusCode == HttpStatusCode.TooManyRequests || // 429 Rate Limit
                r.StatusCode == HttpStatusCode.ServiceUnavailable || // 503 Service Unavailable
                r.StatusCode == HttpStatusCode.RequestTimeout) // 408 Request Timeout
            .WaitAndRetryAsync(
                retryCount: maxRetries,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timeSpan, retryCount, context) =>
                {
                    if (outcome.Exception != null)
                    {
                        logger.LogWarning(
                            "Retry {RetryCount} of {MaxRetries} after {Delay}s due to {ExceptionType}: {Message}",
                            retryCount,
                            maxRetries,
                            timeSpan.TotalSeconds,
                            outcome.Exception.GetType().Name,
                            outcome.Exception.Message
                        );
                    }
                    else if (outcome.Result != null)
                    {
                        logger.LogWarning(
                            "Retry {RetryCount} of {MaxRetries} after {Delay}s due to HTTP {StatusCode}",
                            retryCount,
                            maxRetries,
                            timeSpan.TotalSeconds,
                            (int)outcome.Result.StatusCode
                        );
                    }
                });
    }

    /// <summary>
    /// Creates a retry policy for generic HTTP calls with exponential backoff
    /// </summary>
    /// <param name="logger">Logger for retry attempts</param>
    /// <param name="maxRetries">Maximum number of retry attempts (default: 3)</param>
    /// <returns>Async retry policy</returns>
    public static AsyncRetryPolicy<TResult> CreateGenericRetryPolicy<TResult>(
        ILogger logger,
        int maxRetries = 3)
    {
        return Policy<TResult>
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: maxRetries,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timeSpan, retryCount, context) =>
                {
                    logger.LogWarning(
                        "Retry {RetryCount} of {MaxRetries} after {Delay}s due to {ExceptionType}: {Message}",
                        retryCount,
                        maxRetries,
                        timeSpan.TotalSeconds,
                        outcome.Exception?.GetType().Name ?? "Unknown",
                        outcome.Exception?.Message ?? "Unknown error"
                    );
                });
    }

    /// <summary>
    /// Creates a retry policy specifically for metadata extraction
    /// Handles YoutubeExplode exceptions and network errors
    /// </summary>
    /// <param name="logger">Logger for retry attempts</param>
    /// <param name="maxRetries">Maximum number of retry attempts (default: 3)</param>
    /// <returns>Async retry policy</returns>
    public static AsyncRetryPolicy<TResult> CreateMetadataExtractionPolicy<TResult>(
        ILogger logger,
        int maxRetries = 3)
    {
        return Policy<TResult>
            .Handle<HttpRequestException>(ex =>
            {
                // Retry on network errors, but not on 403 Forbidden (handled separately)
                return ex.StatusCode != HttpStatusCode.Forbidden;
            })
            .Or<TaskCanceledException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: maxRetries,
                sleepDurationProvider: retryAttempt =>
                {
                    // Exponential backoff: 2s, 4s, 8s
                    return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                },
                onRetry: (outcome, timeSpan, retryCount, context) =>
                {
                    var videoId = context.TryGetValue("videoId", out var vid) ? vid : "unknown";

                    logger.LogWarning(
                        outcome.Exception,
                        "Metadata extraction retry {RetryCount} of {MaxRetries} for video {VideoId} after {Delay}s. Error: {Error}",
                        retryCount,
                        maxRetries,
                        videoId,
                        timeSpan.TotalSeconds,
                        outcome.Exception?.Message ?? "Unknown error"
                    );
                });
    }

    /// <summary>
    /// Creates a circuit breaker policy to prevent cascading failures
    /// Opens circuit after consecutive failures, allowing system to recover
    /// </summary>
    /// <param name="logger">Logger for circuit breaker events</param>
    /// <param name="failuresBeforeBreaking">Number of failures before opening circuit (default: 5)</param>
    /// <param name="durationOfBreak">Duration to keep circuit open (default: 30 seconds)</param>
    /// <returns>Async circuit breaker policy</returns>
    public static Polly.CircuitBreaker.AsyncCircuitBreakerPolicy CreateCircuitBreakerPolicy(
        ILogger logger,
        int failuresBeforeBreaking = 5,
        TimeSpan? durationOfBreak = null)
    {
        var breakDuration = durationOfBreak ?? TimeSpan.FromSeconds(30);

        return Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: failuresBeforeBreaking,
                durationOfBreak: breakDuration,
                onBreak: (exception, duration) =>
                {
                    logger.LogError(
                        exception,
                        "Circuit breaker opened. Breaking for {Duration}s after {Failures} consecutive failures",
                        duration.TotalSeconds,
                        failuresBeforeBreaking
                    );
                },
                onReset: () =>
                {
                    logger.LogInformation("Circuit breaker reset. Resuming normal operation");
                },
                onHalfOpen: () =>
                {
                    logger.LogInformation("Circuit breaker half-open. Testing if service has recovered");
                });
    }

    /// <summary>
    /// Creates a timeout policy to prevent hanging requests
    /// </summary>
    /// <param name="logger">Logger for timeout events</param>
    /// <param name="timeout">Timeout duration (default: 30 seconds)</param>
    /// <returns>Async timeout policy</returns>
    public static Polly.Timeout.AsyncTimeoutPolicy CreateTimeoutPolicy(
        ILogger logger,
        TimeSpan? timeout = null)
    {
        var timeoutDuration = timeout ?? TimeSpan.FromSeconds(30);

        return Policy
            .TimeoutAsync(
                timeout: timeoutDuration,
                onTimeoutAsync: (context, span, task) =>
                {
                    logger.LogWarning(
                        "Operation timed out after {Timeout}s. Context: {Context}",
                        span.TotalSeconds,
                        context.OperationKey ?? "Unknown"
                    );
                    return Task.CompletedTask;
                });
    }
}
