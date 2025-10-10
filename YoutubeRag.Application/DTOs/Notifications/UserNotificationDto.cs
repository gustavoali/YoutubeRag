using System;
using System.Collections.Generic;

namespace YoutubeRag.Application.DTOs.Notifications;

/// <summary>
/// Data transfer object for user notifications
/// </summary>
public class UserNotificationDto
{
    /// <summary>
    /// Notification ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Type of notification (Success, Error, Warning, Info)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Notification title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Notification message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Associated job ID (if any)
    /// </summary>
    public string? JobId { get; set; }

    /// <summary>
    /// Associated video ID (if any)
    /// </summary>
    public string? VideoId { get; set; }

    /// <summary>
    /// Whether the notification has been read
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// When the notification was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the notification was read (if read)
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// Additional metadata (error details, action suggestions, etc.)
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
