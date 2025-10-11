namespace YoutubeRag.Application.DTOs.Common;

/// <summary>
/// Represents a validation error for a specific field
/// </summary>
public record ValidationErrorDto
{
    /// <summary>
    /// Gets the name of the field that failed validation
    /// </summary>
    public string Field { get; init; } = string.Empty;

    /// <summary>
    /// Gets the validation error message
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets the attempted value that failed validation
    /// </summary>
    public object? AttemptedValue { get; init; }

    /// <summary>
    /// Gets the validation error code
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Creates a new ValidationErrorDto instance
    /// </summary>
    public ValidationErrorDto(string field, string message, object? attemptedValue = null, string? errorCode = null)
    {
        Field = field;
        Message = message;
        AttemptedValue = attemptedValue;
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Default constructor for deserialization
    /// </summary>
    public ValidationErrorDto()
    {
    }
}
