using YoutubeRag.Application.Interfaces;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace YoutubeRag.Infrastructure.Services;

public class MockVideoProcessingService : IVideoProcessingService
{
    private readonly ILogger<MockVideoProcessingService> _logger;

    public MockVideoProcessingService(ILogger<MockVideoProcessingService> logger)
    {
        _logger = logger;
    }

    public async Task<Video> ProcessVideoFromUrlAsync(string url, string title, string description, string userId)
    {
        _logger.LogInformation("Mock: Processing video from URL: {Url}", url);

        await Task.Delay(1000); // Simulate initial processing delay

        var video = new Video
        {
            Id = Guid.NewGuid().ToString(),
            Title = title ?? "Mock Video - Sample YouTube Content",
            Description = description ?? "This is a mock video for testing the YouTube RAG system.",
            YoutubeId = "dQw4w9WgXcQ",
            YoutubeUrl = url,
            OriginalUrl = url,
            ThumbnailUrl = "https://img.youtube.com/vi/dQw4w9WgXcQ/maxresdefault.jpg",
            Duration = TimeSpan.FromMinutes(3).Add(TimeSpan.FromSeconds(33)),
            ViewCount = 1234567890,
            LikeCount = 12345678,
            Status = VideoStatus.Processing,
            UserId = userId,
            ProcessingProgress = 15,
            ProcessingLog = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}: Mock processing started\n",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Simulate background processing completion
        _ = Task.Run(async () =>
        {
            await Task.Delay(5000); // Simulate total processing time
            video.Status = VideoStatus.Completed;
            video.ProcessingProgress = 100;
            video.ProcessingLog += $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}: Mock processing completed\n";
            video.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Mock: Completed processing for video {VideoId}", video.Id);
        });

        return video;
    }

    public async Task<Video> ProcessVideoFromFileAsync(Stream fileStream, string fileName, string title, string description, string userId)
    {
        _logger.LogInformation("Mock: Processing video from file: {FileName}", fileName);

        await Task.Delay(800); // Simulate file processing delay

        var video = new Video
        {
            Id = Guid.NewGuid().ToString(),
            Title = title ?? Path.GetFileNameWithoutExtension(fileName),
            Description = description ?? "Mock video processed from uploaded file.",
            Status = VideoStatus.Processing,
            FilePath = $"/mock/path/{fileName}",
            UserId = userId,
            ProcessingProgress = 20,
            ProcessingLog = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}: Mock file processing started\n",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Simulate background processing completion
        _ = Task.Run(async () =>
        {
            await Task.Delay(4000); // Simulate processing time
            video.Status = VideoStatus.Completed;
            video.ProcessingProgress = 100;
            video.ProcessingLog += $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}: Mock file processing completed\n";
            video.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Mock: Completed file processing for video {VideoId}", video.Id);
        });

        return video;
    }

    public async Task<VideoProcessingProgress> GetProcessingProgressAsync(string videoId)
    {
        _logger.LogDebug("Mock: Getting processing progress for video: {VideoId}", videoId);

        await Task.Delay(100); // Simulate data retrieval

        // Generate realistic progress based on current time (for demo purposes)
        var random = new Random(videoId.GetHashCode());
        var progress = Math.Min(100, random.Next(60, 101));

        var stages = new List<ProcessingStage>
        {
            new ProcessingStage
            {
                Name = "download",
                Status = progress > 25 ? "completed" : "running",
                Progress = Math.Min(100, Math.Max(0, (progress - 0) * 100 / 25)),
                StartedAt = DateTime.UtcNow.AddMinutes(-10),
                CompletedAt = progress > 25 ? DateTime.UtcNow.AddMinutes(-8) : null
            },
            new ProcessingStage
            {
                Name = "audio_extraction",
                Status = progress > 50 ? "completed" : progress > 25 ? "running" : "pending",
                Progress = Math.Min(100, Math.Max(0, (progress - 25) * 100 / 25)),
                StartedAt = progress > 25 ? DateTime.UtcNow.AddMinutes(-8) : null,
                CompletedAt = progress > 50 ? DateTime.UtcNow.AddMinutes(-6) : null
            },
            new ProcessingStage
            {
                Name = "transcription",
                Status = progress > 80 ? "completed" : progress > 50 ? "running" : "pending",
                Progress = Math.Min(100, Math.Max(0, (progress - 50) * 100 / 30)),
                StartedAt = progress > 50 ? DateTime.UtcNow.AddMinutes(-6) : null,
                CompletedAt = progress > 80 ? DateTime.UtcNow.AddMinutes(-2) : null
            },
            new ProcessingStage
            {
                Name = "embedding",
                Status = progress >= 100 ? "completed" : progress > 80 ? "running" : "pending",
                Progress = Math.Min(100, Math.Max(0, (progress - 80) * 100 / 20)),
                StartedAt = progress > 80 ? DateTime.UtcNow.AddMinutes(-2) : null,
                CompletedAt = progress >= 100 ? DateTime.UtcNow : null
            }
        };

        return new VideoProcessingProgress
        {
            VideoId = videoId,
            Status = progress >= 100 ? VideoStatus.Completed : VideoStatus.Processing,
            OverallProgress = progress,
            CurrentStage = progress switch
            {
                < 25 => "download",
                < 50 => "audio_extraction",
                < 80 => "transcription",
                < 100 => "embedding",
                _ => "completed"
            },
            Stages = stages,
            EstimatedCompletion = progress >= 100 ? null : DateTime.UtcNow.AddMinutes((100 - progress) * 0.1)
        };
    }

    public async Task<bool> CancelProcessingAsync(string videoId)
    {
        _logger.LogInformation("Mock: Cancelling processing for video: {VideoId}", videoId);

        await Task.Delay(200); // Simulate cancellation time

        return true;
    }
}