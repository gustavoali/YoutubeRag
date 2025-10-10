namespace YoutubeRag.Application.Interfaces;

public interface IYouTubeService
{
    Task<YouTubeVideoInfo> GetVideoInfoAsync(string url);
    Task<string> DownloadVideoAsync(string url, string outputPath);
    Task<string> DownloadAudioAsync(string url, string outputPath);
    Task<bool> IsValidYouTubeUrlAsync(string url);
    string ExtractVideoIdFromUrl(string url);
}

public class YouTubeVideoInfo
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public int ViewCount { get; set; }
    public int LikeCount { get; set; }
    public string ChannelName { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
    public List<string> Tags { get; set; } = new();
}