using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Domain.Entities;

namespace YoutubeRag.Api.Hubs;

/// <summary>
/// SignalR Hub para notificaciones de progreso de jobs en tiempo real
/// </summary>
[Authorize]
public class JobProgressHub : Hub
{
    private readonly IJobRepository _jobRepository;
    private readonly IVideoRepository _videoRepository;
    private readonly ILogger<JobProgressHub> _logger;

    public JobProgressHub(
        IJobRepository jobRepository,
        IVideoRepository videoRepository,
        ILogger<JobProgressHub> logger)
    {
        _jobRepository = jobRepository;
        _videoRepository = videoRepository;
        _logger = logger;
    }

    /// <summary>
    /// Evento ejecutado cuando un cliente se conecta al hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("SignalR client connected: {ConnectionId}, User: {UserId}",
            Context.ConnectionId, userId);

        if (!string.IsNullOrEmpty(userId))
        {
            // Agregar a grupo de usuario para notificaciones broadcast
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
            _logger.LogDebug("Added connection {ConnectionId} to user group: user-{UserId}",
                Context.ConnectionId, userId);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Evento ejecutado cuando un cliente se desconecta del hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (exception != null)
        {
            _logger.LogWarning(exception,
                "SignalR client disconnected with error: {ConnectionId}, User: {UserId}",
                Context.ConnectionId, userId);
        }
        else
        {
            _logger.LogInformation("SignalR client disconnected: {ConnectionId}, User: {UserId}",
                Context.ConnectionId, userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Suscribe el cliente a las notificaciones de un job específico
    /// </summary>
    /// <param name="jobId">ID del job</param>
    public async Task SubscribeToJob(string jobId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"job-{jobId}");
        _logger.LogInformation("Client {ConnectionId} subscribed to job {JobId}",
            Context.ConnectionId, jobId);

        // Enviar estado actual del job
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job != null)
        {
            await Clients.Caller.SendAsync("JobProgressUpdate", MapJobToDto(job));
        }
        else
        {
            _logger.LogWarning("Job {JobId} not found for subscription", jobId);
            await Clients.Caller.SendAsync("Error", new
            {
                code = "NOT_FOUND",
                message = $"Job {jobId} not found"
            });
        }
    }

    /// <summary>
    /// Desuscribe el cliente de las notificaciones de un job específico
    /// </summary>
    /// <param name="jobId">ID del job</param>
    public async Task UnsubscribeFromJob(string jobId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"job-{jobId}");
        _logger.LogInformation("Client {ConnectionId} unsubscribed from job {JobId}",
            Context.ConnectionId, jobId);
    }

    /// <summary>
    /// Suscribe el cliente a las notificaciones de un video específico
    /// </summary>
    /// <param name="videoId">ID del video</param>
    public async Task SubscribeToVideo(string videoId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"video-{videoId}");
        _logger.LogInformation("Client {ConnectionId} subscribed to video {VideoId}",
            Context.ConnectionId, videoId);

        // Enviar estado actual del video
        var video = await _videoRepository.GetByIdAsync(videoId);
        if (video != null)
        {
            await Clients.Caller.SendAsync("VideoProgressUpdate", MapVideoToDto(video));
        }
        else
        {
            _logger.LogWarning("Video {VideoId} not found for subscription", videoId);
            await Clients.Caller.SendAsync("Error", new
            {
                code = "NOT_FOUND",
                message = $"Video {videoId} not found"
            });
        }
    }

    /// <summary>
    /// Desuscribe el cliente de las notificaciones de un video específico
    /// </summary>
    /// <param name="videoId">ID del video</param>
    public async Task UnsubscribeFromVideo(string videoId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"video-{videoId}");
        _logger.LogInformation("Client {ConnectionId} unsubscribed from video {VideoId}",
            Context.ConnectionId, videoId);
    }

    /// <summary>
    /// Obtiene el progreso actual de un job
    /// </summary>
    /// <param name="jobId">ID del job</param>
    public async Task GetJobProgress(string jobId)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job != null)
        {
            await Clients.Caller.SendAsync("JobProgressUpdate", MapJobToDto(job));
        }
        else
        {
            _logger.LogWarning("Job {JobId} not found", jobId);
            await Clients.Caller.SendAsync("Error", new
            {
                code = "NOT_FOUND",
                message = $"Job {jobId} not found"
            });
        }
    }

    /// <summary>
    /// Obtiene el progreso actual de un video
    /// </summary>
    /// <param name="videoId">ID del video</param>
    public async Task GetVideoProgress(string videoId)
    {
        var video = await _videoRepository.GetByIdAsync(videoId);
        if (video != null)
        {
            await Clients.Caller.SendAsync("VideoProgressUpdate", MapVideoToDto(video));
        }
        else
        {
            _logger.LogWarning("Video {VideoId} not found", videoId);
            await Clients.Caller.SendAsync("Error", new
            {
                code = "NOT_FOUND",
                message = $"Video {videoId} not found"
            });
        }
    }

    /// <summary>
    /// Mapea un Job entity a un DTO para envío por SignalR
    /// </summary>
    private object MapJobToDto(Job job)
    {
        return new
        {
            jobId = job.Id,
            videoId = job.VideoId,
            type = job.Type.ToString(),
            status = job.Status.ToString(),
            progress = job.Progress,
            statusMessage = job.StatusMessage,
            errorMessage = job.ErrorMessage,
            createdAt = job.CreatedAt,
            updatedAt = job.UpdatedAt,
            completedAt = job.CompletedAt
        };
    }

    /// <summary>
    /// Mapea un Video entity a un DTO para envío por SignalR
    /// </summary>
    private object MapVideoToDto(Video video)
    {
        return new
        {
            videoId = video.Id,
            processingStatus = video.ProcessingStatus.ToString(),
            transcriptionStatus = video.TranscriptionStatus.ToString(),
            embeddingStatus = video.EmbeddingStatus.ToString(),
            processingProgress = video.ProcessingProgress,
            embeddingProgress = video.EmbeddingProgress,
            updatedAt = video.UpdatedAt
        };
    }
}
