using System;
using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Application.Services;

/// <summary>
/// Formats technical exception messages into user-friendly error messages based on failure categories
/// </summary>
public static class ErrorMessageFormatter
{
    /// <summary>
    /// Formats an exception into a user-friendly message based on the failure category
    /// </summary>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="category">The categorized failure type</param>
    /// <returns>A user-friendly error message</returns>
    public static string FormatUserFriendlyMessage(Exception exception, FailureCategory category)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return category switch
        {
            FailureCategory.TransientNetworkError => GetNetworkErrorMessage(exception),
            FailureCategory.ResourceNotAvailable => GetResourceErrorMessage(exception),
            FailureCategory.PermanentError => GetPermanentErrorMessage(exception),
            FailureCategory.UnknownError => GetGenericErrorMessage(exception),
            _ => "An unexpected error occurred while processing your video."
        };
    }

    /// <summary>
    /// Gets a user-friendly message for transient network errors
    /// </summary>
    private static string GetNetworkErrorMessage(Exception ex)
    {
        var exceptionType = ex.GetType().Name;
        var message = ex.Message.ToLowerInvariant();

        // HTTP-related errors
        if (exceptionType == "HttpRequestException" || message.Contains("http"))
        {
            if (message.Contains("ssl") || message.Contains("certificate"))
            {
                return "Unable to establish a secure connection. This is usually temporary. Retrying automatically...";
            }

            if (message.Contains("timeout") || message.Contains("timed out"))
            {
                return "Connection timed out while downloading video. Retrying automatically...";
            }

            if (message.Contains("connection") || message.Contains("network"))
            {
                return "Network connection issue detected. Retrying automatically...";
            }

            return "Unable to download video due to network issues. Retrying automatically...";
        }

        // Timeout errors
        if (exceptionType == "TimeoutException" || message.Contains("timeout"))
        {
            return "Video download timed out. Retrying automatically...";
        }

        // YouTube API errors
        if (message.Contains("youtube") || message.Contains("video unavailable"))
        {
            return "YouTube service is temporarily unavailable. Retrying automatically...";
        }

        // Generic network error
        return "Temporary network issue detected. The system will retry automatically...";
    }

    /// <summary>
    /// Gets a user-friendly message for resource availability errors
    /// </summary>
    private static string GetResourceErrorMessage(Exception ex)
    {
        var exceptionType = ex.GetType().Name;
        var message = ex.Message.ToLowerInvariant();

        // Disk space errors
        if (exceptionType == "IOException" && (message.Contains("disk") || message.Contains("space")))
        {
            return "Insufficient storage space on server. Please contact support to increase your quota.";
        }

        // Whisper model errors
        if (message.Contains("model") || message.Contains("whisper"))
        {
            if (message.Contains("download") || message.Contains("loading"))
            {
                return "AI transcription model is currently loading. Your video will be processed automatically once the model is ready.";
            }

            if (message.Contains("not found") || message.Contains("missing"))
            {
                return "AI transcription model is being prepared. Your video will be processed shortly.";
            }

            return "AI transcription model is temporarily unavailable. Retrying automatically...";
        }

        // Memory errors
        if (exceptionType == "OutOfMemoryException" || message.Contains("memory"))
        {
            return "Server is currently processing high load. Your video will be processed when resources are available.";
        }

        // File lock errors
        if (message.Contains("locked") || message.Contains("in use") || message.Contains("access denied"))
        {
            return "File is temporarily locked. The system will retry automatically...";
        }

        // Generic resource error
        return "Required resources are temporarily unavailable. Your video will be processed automatically when resources are ready.";
    }

    /// <summary>
    /// Gets a user-friendly message for permanent errors
    /// </summary>
    private static string GetPermanentErrorMessage(Exception ex)
    {
        var message = ex.Message.ToLowerInvariant();

        // Video unavailable errors
        if (message.Contains("deleted") || message.Contains("removed") || message.Contains("no longer available"))
        {
            return "This video is no longer available on YouTube. It may have been deleted or removed by the owner.";
        }

        // Private/restricted video errors
        if (message.Contains("private") || message.Contains("restricted"))
        {
            return "This video is private or restricted and cannot be accessed. Please verify the video permissions.";
        }

        // Age-restricted errors
        if (message.Contains("age") && message.Contains("restricted"))
        {
            return "This video is age-restricted and cannot be processed automatically.";
        }

        // Copyright errors
        if (message.Contains("copyright") || message.Contains("dmca"))
        {
            return "This video has copyright restrictions and cannot be processed.";
        }

        // Invalid URL/format errors
        if (message.Contains("invalid") && (message.Contains("url") || message.Contains("format") || message.Contains("id")))
        {
            return "Invalid YouTube URL or video ID provided. Please verify the video link is correct.";
        }

        // Unsupported format errors
        if (message.Contains("unsupported") || message.Contains("not supported"))
        {
            return "This video format is not supported for transcription.";
        }

        // Geographic restrictions
        if (message.Contains("region") || message.Contains("country") || message.Contains("geographic"))
        {
            return "This video is not available in your region due to geographic restrictions.";
        }

        // Generic permanent error
        return "This video cannot be processed. Please verify the video URL is correct and the video is publicly accessible.";
    }

    /// <summary>
    /// Gets a generic user-friendly message for unknown errors
    /// </summary>
    private static string GetGenericErrorMessage(Exception ex)
    {
        var exceptionType = ex.GetType().Name;

        // Null reference - generic error
        if (exceptionType == "NullReferenceException")
        {
            return "We encountered an unexpected error while processing your video. Our team has been notified.";
        }

        // Argument errors - likely configuration issue
        if (exceptionType == "ArgumentException" || exceptionType == "ArgumentNullException")
        {
            return "Invalid configuration detected. Please contact support.";
        }

        // Operation cancelled
        if (exceptionType == "OperationCanceledException" || exceptionType == "TaskCanceledException")
        {
            return "Processing was cancelled. You can try processing this video again.";
        }

        // Generic unknown error
        return "We encountered an error while processing your video. Our team has been notified and will investigate.";
    }

    /// <summary>
    /// Gets a technical summary for logging/debugging purposes (not user-facing)
    /// </summary>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="category">The categorized failure type</param>
    /// <returns>Technical summary string</returns>
    public static string GetTechnicalSummary(Exception exception, FailureCategory category)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return $"[{category}] {exception.GetType().Name}: {exception.Message}";
    }
}
