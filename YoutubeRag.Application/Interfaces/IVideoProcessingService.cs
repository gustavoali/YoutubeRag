using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Application.Interfaces;

public interface IVideoProcessingService
{
    Task<Video> ProcessVideoFromUrlAsync(string url, string title, string description, string userId);
    Task<Video> ProcessVideoFromFileAsync(Stream fileStream, string fileName, string title, string description, string userId);
    Task<VideoProcessingProgress> GetProcessingProgressAsync(string videoId);
    Task<bool> CancelProcessingAsync(string videoId);
}

public class VideoProcessingProgress
{
    public string VideoId { get; set; } = string.Empty;
    public VideoStatus Status { get; set; }
    public int OverallProgress { get; set; } // 0-100
    public string CurrentStage { get; set; } = string.Empty;
    public List<ProcessingStage> Stages { get; set; } = new();
    public DateTime? EstimatedCompletion { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ProcessingStage
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // pending, running, completed, failed
    public int Progress { get; set; } // 0-100
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}