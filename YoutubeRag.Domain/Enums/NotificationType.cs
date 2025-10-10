namespace YoutubeRag.Domain.Enums;

/// <summary>
/// Represents the type of notification to be displayed to the user
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// Success notification (green/positive)
    /// </summary>
    Success = 0,

    /// <summary>
    /// Error notification (red/negative)
    /// </summary>
    Error = 1,

    /// <summary>
    /// Warning notification (yellow/caution)
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Informational notification (blue/neutral)
    /// </summary>
    Info = 3
}
