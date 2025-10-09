namespace YoutubeRag.Domain.Enums;

/// <summary>
/// Categorizes different types of job failures for appropriate retry strategies
/// </summary>
public enum FailureCategory
{
    /// <summary>
    /// Transient network or API errors (e.g., YouTube API timeout, HTTP 5xx)
    /// Should be retried with exponential backoff
    /// </summary>
    TransientNetworkError = 0,

    /// <summary>
    /// Resource temporarily unavailable (e.g., Whisper model downloading, disk full)
    /// Should be retried with linear backoff to wait for resource
    /// </summary>
    ResourceNotAvailable = 1,

    /// <summary>
    /// Permanent error that won't be resolved by retrying (e.g., video deleted, invalid format)
    /// Should go directly to Dead Letter Queue without retry
    /// </summary>
    PermanentError = 2,

    /// <summary>
    /// Unknown or unclassified error
    /// Should be retried cautiously with limited attempts
    /// </summary>
    UnknownError = 3
}
