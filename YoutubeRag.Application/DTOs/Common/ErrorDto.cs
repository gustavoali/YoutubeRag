namespace YoutubeRag.Application.DTOs.Common;

/// <summary>
/// Represents an error response
/// </summary>
public record ErrorDto
{
    /// <summary>
    /// Gets the error code
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// Gets the error message
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets additional details about the error
    /// </summary>
    public string? Details { get; init; }

    /// <summary>
    /// Gets the timestamp when the error occurred
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the trace identifier for debugging
    /// </summary>
    public string? TraceId { get; init; }

    /// <summary>
    /// Creates a new ErrorDto instance
    /// </summary>
    public ErrorDto(string code, string message, string? details = null)
    {
        Code = code;
        Message = message;
        Details = details;
    }

    /// <summary>
    /// Default constructor for deserialization
    /// </summary>
    public ErrorDto()
    {
    }
}