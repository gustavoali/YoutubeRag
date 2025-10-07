namespace YoutubeRag.Application.DTOs.Video;

public class VideoUrlRequest
{
    public string Url { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public ProcessingPriority Priority { get; set; } = ProcessingPriority.Normal;
}

public enum ProcessingPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}