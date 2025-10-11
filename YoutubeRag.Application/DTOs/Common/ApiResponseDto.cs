namespace YoutubeRag.Application.DTOs.Common;

/// <summary>
/// Generic API response wrapper for all API responses
/// </summary>
/// <typeparam name="T">The type of data in the response</typeparam>
public record ApiResponseDto<T>
{
    /// <summary>
    /// Gets a value indicating whether the request was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the response data
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Gets the error message if the request failed
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Gets the list of validation errors if any
    /// </summary>
    public IReadOnlyList<ValidationErrorDto> Errors { get; init; } = new List<ValidationErrorDto>();

    /// <summary>
    /// Gets the timestamp of the response
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the trace identifier for debugging
    /// </summary>
    public string? TraceId { get; init; }

    /// <summary>
    /// Creates a successful response with data
    /// </summary>
    public static ApiResponseDto<T> SuccessResponse(T data, string? message = null)
    {
        return new ApiResponseDto<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    /// <summary>
    /// Creates a failed response with an error message
    /// </summary>
    public static ApiResponseDto<T> FailureResponse(string message, IEnumerable<ValidationErrorDto>? errors = null)
    {
        return new ApiResponseDto<T>
        {
            Success = false,
            Message = message,
            Errors = errors?.ToList() ?? new List<ValidationErrorDto>()
        };
    }

    /// <summary>
    /// Default constructor for deserialization
    /// </summary>
    public ApiResponseDto()
    {
    }
}
