using YoutubeRag.Application.DTOs.Progress;

namespace YoutubeRag.Application.Interfaces.Services;

/// <summary>
/// Servicio para notificaciones de progreso en tiempo real via SignalR
/// </summary>
public interface IProgressNotificationService
{
    /// <summary>
    /// Notifica el progreso de un job específico
    /// </summary>
    /// <param name="jobId">ID del job</param>
    /// <param name="progress">Datos de progreso del job</param>
    Task NotifyJobProgressAsync(string jobId, JobProgressDto progress);

    /// <summary>
    /// Notifica que un job ha completado exitosamente
    /// </summary>
    /// <param name="jobId">ID del job</param>
    /// <param name="videoId">ID del video asociado</param>
    /// <param name="status">Estado final del job</param>
    Task NotifyJobCompletedAsync(string jobId, string videoId, string status);

    /// <summary>
    /// Notifica que un job ha fallado
    /// </summary>
    /// <param name="jobId">ID del job</param>
    /// <param name="videoId">ID del video asociado</param>
    /// <param name="error">Mensaje de error</param>
    Task NotifyJobFailedAsync(string jobId, string videoId, string error);

    /// <summary>
    /// Notifica el progreso general de un video
    /// </summary>
    /// <param name="videoId">ID del video</param>
    /// <param name="progress">Datos de progreso del video</param>
    Task NotifyVideoProgressAsync(string videoId, VideoProgressDto progress);

    /// <summary>
    /// Notifica a un usuario específico
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="notification">Datos de la notificación</param>
    Task NotifyUserAsync(string userId, UserNotificationDto notification);

    /// <summary>
    /// Notifica a todos los usuarios conectados (broadcast)
    /// </summary>
    /// <param name="notification">Datos de la notificación</param>
    Task BroadcastNotificationAsync(UserNotificationDto notification);
}
