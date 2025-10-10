namespace YoutubeRag.Application.DTOs.Progress;

/// <summary>
/// DTO para notificaciones genéricas a usuarios
/// </summary>
public class UserNotificationDto
{
    /// <summary>
    /// Tipo de notificación
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Mensaje de la notificación
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Datos adicionales de la notificación
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Timestamp de la notificación
    /// </summary>
    public DateTime Timestamp { get; set; }
}
