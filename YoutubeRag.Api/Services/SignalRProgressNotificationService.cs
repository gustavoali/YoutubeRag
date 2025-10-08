using Microsoft.AspNetCore.SignalR;
using YoutubeRag.Api.Hubs;
using YoutubeRag.Application.DTOs.Progress;
using YoutubeRag.Application.Interfaces.Services;

namespace YoutubeRag.Api.Services;

/// <summary>
/// Implementación de servicio de notificaciones de progreso usando SignalR
/// </summary>
public class SignalRProgressNotificationService : IProgressNotificationService
{
    private readonly IHubContext<JobProgressHub> _hubContext;
    private readonly ILogger<SignalRProgressNotificationService> _logger;

    public SignalRProgressNotificationService(
        IHubContext<JobProgressHub> hubContext,
        ILogger<SignalRProgressNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Notifica el progreso de un job específico
    /// </summary>
    public async Task NotifyJobProgressAsync(string jobId, JobProgressDto progress)
    {
        try
        {
            _logger.LogDebug("Notifying job progress: {JobId}, Progress: {Progress}%, Stage: {Stage}",
                jobId, progress.Progress, progress.CurrentStage);

            // Notificar al grupo del job específico
            await _hubContext.Clients.Group($"job-{jobId}")
                .SendAsync("JobProgressUpdate", progress);

            // Notificar al grupo del video (si existe)
            if (!string.IsNullOrEmpty(progress.VideoId))
            {
                await _hubContext.Clients.Group($"video-{progress.VideoId}")
                    .SendAsync("JobProgressUpdate", progress);
            }

            _logger.LogTrace("Job progress notification sent successfully: {JobId}", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying job progress: {JobId}", jobId);
            // No lanzamos la excepción para no interrumpir el flujo del job
        }
    }

    /// <summary>
    /// Notifica que un job ha completado exitosamente
    /// </summary>
    public async Task NotifyJobCompletedAsync(string jobId, string videoId, string status)
    {
        try
        {
            _logger.LogInformation("Notifying job completed: {JobId}, VideoId: {VideoId}, Status: {Status}",
                jobId, videoId, status);

            var notification = new
            {
                jobId,
                videoId,
                status,
                message = "Job completed successfully",
                completedAt = DateTime.UtcNow
            };

            // Notificar al grupo del job
            await _hubContext.Clients.Group($"job-{jobId}")
                .SendAsync("JobCompleted", notification);

            // Notificar al grupo del video
            if (!string.IsNullOrEmpty(videoId))
            {
                await _hubContext.Clients.Group($"video-{videoId}")
                    .SendAsync("JobCompleted", notification);
            }

            _logger.LogTrace("Job completion notification sent successfully: {JobId}", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying job completion: {JobId}", jobId);
        }
    }

    /// <summary>
    /// Notifica que un job ha fallado
    /// </summary>
    public async Task NotifyJobFailedAsync(string jobId, string videoId, string error)
    {
        try
        {
            _logger.LogWarning("Notifying job failed: {JobId}, VideoId: {VideoId}, Error: {Error}",
                jobId, videoId, error);

            var notification = new
            {
                jobId,
                videoId,
                error,
                message = "Job failed",
                failedAt = DateTime.UtcNow
            };

            // Notificar al grupo del job
            await _hubContext.Clients.Group($"job-{jobId}")
                .SendAsync("JobFailed", notification);

            // Notificar al grupo del video
            if (!string.IsNullOrEmpty(videoId))
            {
                await _hubContext.Clients.Group($"video-{videoId}")
                    .SendAsync("JobFailed", notification);
            }

            _logger.LogTrace("Job failure notification sent successfully: {JobId}", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying job failure: {JobId}", jobId);
        }
    }

    /// <summary>
    /// Notifica el progreso general de un video
    /// </summary>
    public async Task NotifyVideoProgressAsync(string videoId, VideoProgressDto progress)
    {
        try
        {
            _logger.LogDebug("Notifying video progress: {VideoId}, Progress: {Progress}%",
                videoId, progress.OverallProgress);

            // Notificar al grupo del video
            await _hubContext.Clients.Group($"video-{videoId}")
                .SendAsync("VideoProgressUpdate", progress);

            _logger.LogTrace("Video progress notification sent successfully: {VideoId}", videoId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying video progress: {VideoId}", videoId);
        }
    }

    /// <summary>
    /// Notifica a un usuario específico
    /// </summary>
    public async Task NotifyUserAsync(string userId, UserNotificationDto notification)
    {
        try
        {
            _logger.LogDebug("Notifying user: {UserId}, Type: {Type}, Message: {Message}",
                userId, notification.Type, notification.Message);

            // Notificar al grupo del usuario
            await _hubContext.Clients.Group($"user-{userId}")
                .SendAsync("UserNotification", notification);

            _logger.LogTrace("User notification sent successfully: {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying user: {UserId}", userId);
        }
    }

    /// <summary>
    /// Notifica a todos los usuarios conectados (broadcast)
    /// </summary>
    public async Task BroadcastNotificationAsync(UserNotificationDto notification)
    {
        try
        {
            _logger.LogInformation("Broadcasting notification: {Type}, Message: {Message}",
                notification.Type, notification.Message);

            // Notificar a todos los clientes conectados
            await _hubContext.Clients.All
                .SendAsync("BroadcastNotification", notification);

            _logger.LogTrace("Broadcast notification sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting notification");
        }
    }
}
