using Microsoft.AspNetCore.SignalR;
using YoutubeRag.Api.Hubs;
using YoutubeRag.Application.DTOs.Progress;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Application.Interfaces.Services;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Api.Services;

/// <summary>
/// Implementación de servicio de notificaciones de progreso usando SignalR
/// </summary>
public class SignalRProgressNotificationService : IProgressNotificationService
{
    private readonly IHubContext<JobProgressHub> _hubContext;
    private readonly IUserNotificationRepository _notificationRepository;
    private readonly IJobRepository _jobRepository;
    private readonly ILogger<SignalRProgressNotificationService> _logger;

    public SignalRProgressNotificationService(
        IHubContext<JobProgressHub> hubContext,
        IUserNotificationRepository notificationRepository,
        IJobRepository jobRepository,
        ILogger<SignalRProgressNotificationService> logger)
    {
        _hubContext = hubContext;
        _notificationRepository = notificationRepository;
        _jobRepository = jobRepository;
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

            // GAP-3: Persist notification to database
            var persistedNotification = new UserNotification
            {
                UserId = null,  // Broadcast (could be enhanced to get from job.UserId if available)
                Type = NotificationType.Success,
                Title = "Video Processing Complete",
                Message = "Your video has been successfully transcribed and is ready for search.",
                JobId = jobId,
                VideoId = videoId,
                Metadata = new Dictionary<string, object>
                {
                    { "action", "view_video" },
                    { "actionUrl", $"/videos/{videoId}" },
                    { "status", status }
                }
            };

            await _notificationRepository.AddAsync(persistedNotification);

            var notification = new
            {
                jobId,
                videoId,
                status,
                message = persistedNotification.Message,
                completedAt = DateTime.UtcNow,
                notificationId = persistedNotification.Id
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

            _logger.LogTrace("Job completion notification sent and persisted successfully: {JobId}, NotificationId: {NotificationId}",
                jobId, persistedNotification.Id);
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

            // Get job details for enhanced error information
            var job = await _jobRepository.GetByIdAsync(jobId);

            // GAP-3 & GAP-6: Persist notification with error details and action suggestions
            var persistedNotification = new UserNotification
            {
                UserId = null,  // Broadcast
                Type = NotificationType.Error,
                Title = "Video Processing Failed",
                Message = error,  // User-friendly message from ErrorMessageFormatter
                JobId = jobId,
                VideoId = videoId,
                Metadata = new Dictionary<string, object>
                {
                    { "errorType", job?.ErrorType ?? "Unknown" },
                    { "failedStage", job?.FailedStage?.ToString() ?? "Unknown" },
                    { "failureCategory", job?.LastFailureCategory ?? "Unknown" },
                    { "action", "retry" },
                    { "actionSuggestion", GetActionSuggestion(job) },
                    { "retryCount", job?.RetryCount ?? 0 },
                    { "maxRetries", job?.MaxRetries ?? 3 }
                }
            };

            await _notificationRepository.AddAsync(persistedNotification);

            var notification = new
            {
                jobId,
                videoId,
                error,
                message = "Job failed",
                failedAt = DateTime.UtcNow,
                notificationId = persistedNotification.Id,
                metadata = persistedNotification.Metadata
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

            _logger.LogTrace("Job failure notification sent and persisted successfully: {JobId}, NotificationId: {NotificationId}",
                jobId, persistedNotification.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying job failure: {JobId}", jobId);
        }
    }

    /// <summary>
    /// GAP-6: Gets action suggestion based on job failure category
    /// </summary>
    private string GetActionSuggestion(Job? job)
    {
        if (job == null)
        {
            return "Please try again later.";
        }

        return job.LastFailureCategory switch
        {
            "TransientNetworkError" => "This is a temporary network issue. The system will retry automatically.",
            "ResourceNotAvailable" => "Waiting for resources to become available. Please check back in a few minutes.",
            "PermanentError" => "This video cannot be processed. Please verify the video URL is correct and the video is publicly accessible.",
            _ => "Please contact support if the issue persists."
        };
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
