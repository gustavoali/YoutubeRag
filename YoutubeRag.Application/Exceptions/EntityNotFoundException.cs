namespace YoutubeRag.Application.Exceptions;

/// <summary>
/// Exception thrown when a requested entity is not found in the database
/// </summary>
public class EntityNotFoundException : Exception
{
    public string EntityName { get; }
    public string EntityId { get; }

    public EntityNotFoundException(string entityName, string entityId)
        : base($"{entityName} with id '{entityId}' was not found")
    {
        EntityName = entityName;
        EntityId = entityId;
    }

    public EntityNotFoundException(string entityName, string entityId, string message)
        : base(message)
    {
        EntityName = entityName;
        EntityId = entityId;
    }
}
