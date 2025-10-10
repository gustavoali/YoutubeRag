namespace YoutubeRag.Application.Exceptions;

/// <summary>
/// Exception thrown when a database operation fails
/// </summary>
public class DatabaseException : Exception
{
    public DatabaseException(string message)
        : base(message)
    {
    }

    public DatabaseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
