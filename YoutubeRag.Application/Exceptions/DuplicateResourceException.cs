namespace YoutubeRag.Application.Exceptions;

/// <summary>
/// Exception thrown when attempting to create a resource that already exists
/// </summary>
public class DuplicateResourceException : Exception
{
    public string ResourceId { get; }
    public string ResourceType { get; }

    public DuplicateResourceException(string resourceType, string resourceId)
        : base($"{resourceType} already exists with ID: {resourceId}")
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }

    public DuplicateResourceException(string resourceType, string resourceId, string message)
        : base(message)
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }
}
