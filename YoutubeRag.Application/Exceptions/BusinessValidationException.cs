namespace YoutubeRag.Application.Exceptions;

/// <summary>
/// Exception thrown when business validation rules are violated
/// </summary>
public class BusinessValidationException : Exception
{
    public Dictionary<string, string[]> Errors { get; }

    public BusinessValidationException(string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public BusinessValidationException(string message, Dictionary<string, string[]> errors)
        : base(message)
    {
        Errors = errors;
    }

    public BusinessValidationException(string field, string error)
        : base($"Validation failed for {field}")
    {
        Errors = new Dictionary<string, string[]>
        {
            { field, new[] { error } }
        };
    }
}
