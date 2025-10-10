using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Domain.Entities;

/// <summary>
/// Represents a persistent notification for users with metadata for actions and error details
/// </summary>
public class UserNotification : BaseEntity
{
    /// <summary>
    /// User ID this notification belongs to (null = broadcast to all users)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Type of notification (Success, Error, Warning, Info)
    /// </summary>
    public NotificationType Type { get; set; } = NotificationType.Info;

    /// <summary>
    /// Notification title (short summary)
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed notification message (user-friendly)
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Associated job ID (optional)
    /// </summary>
    public string? JobId { get; set; }

    /// <summary>
    /// Associated video ID (optional)
    /// </summary>
    public string? VideoId { get; set; }

    /// <summary>
    /// Whether the notification has been read by the user
    /// </summary>
    public bool IsRead { get; set; } = false;

    /// <summary>
    /// When the notification was read
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// JSON-serialized metadata (error details, action suggestions, etc.)
    /// </summary>
    public string? MetadataJson { get; set; }

    // Navigation properties
    /// <summary>
    /// Associated job (if any)
    /// </summary>
    public virtual Job? Job { get; set; }

    /// <summary>
    /// Associated video (if any)
    /// </summary>
    public virtual Video? Video { get; set; }

    /// <summary>
    /// Associated user (if any - null for broadcasts)
    /// </summary>
    public virtual User? User { get; set; }

    /// <summary>
    /// Helper property to access metadata as a dictionary
    /// </summary>
    [NotMapped]
    public Dictionary<string, object>? Metadata
    {
        get
        {
            if (string.IsNullOrEmpty(MetadataJson))
                return null;

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(MetadataJson);
            }
            catch
            {
                return null;
            }
        }
        set
        {
            MetadataJson = value == null
                ? null
                : JsonSerializer.Serialize(value);
        }
    }

    /// <summary>
    /// Marks the notification as read
    /// </summary>
    public void MarkAsRead()
    {
        IsRead = true;
        ReadAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
