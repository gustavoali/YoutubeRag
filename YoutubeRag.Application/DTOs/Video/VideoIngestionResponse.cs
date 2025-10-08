namespace YoutubeRag.Application.DTOs.Video;

public class VideoIngestionResponse
{
    public string VideoId { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public string YouTubeId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string? ProgressUrl { get; set; }
}