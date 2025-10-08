namespace YoutubeRag.Application.Exceptions;

/// <summary>
/// Exception thrown when a user is not authorized to perform an action
/// </summary>
public class UnauthorizedException : Exception
{
    public string UserId { get; }
    public string Action { get; }

    public UnauthorizedException(string message)
        : base(message)
    {
        UserId = string.Empty;
        Action = string.Empty;
    }

    public UnauthorizedException(string userId, string action)
        : base($"User '{userId}' is not authorized to perform action '{action}'")
    {
        UserId = userId;
        Action = action;
    }

    public UnauthorizedException(string userId, string action, string message)
        : base(message)
    {
        UserId = userId;
        Action = action;
    }
}
