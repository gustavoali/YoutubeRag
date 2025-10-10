using Microsoft.Extensions.Logging;
using YoutubeRag.Application.DTOs.Progress;
using YoutubeRag.Application.Interfaces.Services;

namespace YoutubeRag.Infrastructure.Services.Mock;

/// <summary>
/// Implementación mock del servicio de notificaciones de progreso para testing
/// </summary>
public class MockProgressNotificationService : IProgressNotificationService
{
    private readonly ILogger<MockProgressNotificationService> _logger;

    public MockProgressNotificationService(ILogger<MockProgressNotificationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Mock: Notifica el progreso de un job específico
    /// </summary>
    public Task NotifyJobProgressAsync(string jobId, JobProgressDto progress)
    {
        _logger.LogInformation(
            "Mock notification - Job progress: JobId={JobId}, Progress={Progress}%, Stage={Stage}, Status={Status}",
            jobId, progress.Progress, progress.CurrentStage, progress.Status);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Mock: Notifica que un job ha completado
    /// </summary>
    public Task NotifyJobCompletedAsync(string jobId, string videoId, string status)
    {
        _logger.LogInformation(
            "Mock notification - Job completed: JobId={JobId}, VideoId={VideoId}, Status={Status}",
            jobId, videoId, status);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Mock: Notifica que un job ha fallado
    /// </summary>
    public Task NotifyJobFailedAsync(string jobId, string videoId, string error)
    {
        _logger.LogWarning(
            "Mock notification - Job failed: JobId={JobId}, VideoId={VideoId}, Error={Error}",
            jobId, videoId, error);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Mock: Notifica el progreso general de un video
    /// </summary>
    public Task NotifyVideoProgressAsync(string videoId, VideoProgressDto progress)
    {
        _logger.LogInformation(
            "Mock notification - Video progress: VideoId={VideoId}, OverallProgress={Progress}%, ProcessingStatus={ProcessingStatus}",
            videoId, progress.OverallProgress, progress.ProcessingStatus);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Mock: Notifica a un usuario específico
    /// </summary>
    public Task NotifyUserAsync(string userId, UserNotificationDto notification)
    {
        _logger.LogInformation(
            "Mock notification - User notification: UserId={UserId}, Type={Type}, Message={Message}",
            userId, notification.Type, notification.Message);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Mock: Notifica a todos los usuarios conectados (broadcast)
    /// </summary>
    public Task BroadcastNotificationAsync(UserNotificationDto notification)
    {
        _logger.LogInformation(
            "Mock notification - Broadcast: Type={Type}, Message={Message}",
            notification.Type, notification.Message);

        return Task.CompletedTask;
    }
}
