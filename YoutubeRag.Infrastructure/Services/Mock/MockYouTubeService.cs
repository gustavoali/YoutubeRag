using YoutubeRag.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace YoutubeRag.Infrastructure.Services;

public class MockYouTubeService : IYouTubeService
{
    private readonly ILogger<MockYouTubeService> _logger;

    public MockYouTubeService(ILogger<MockYouTubeService> logger)
    {
        _logger = logger;
    }

    public async Task<YouTubeVideoInfo> GetVideoInfoAsync(string url)
    {
        _logger.LogInformation("Mock: Getting video info for URL: {Url}", url);

        await Task.Delay(500); // Simulate API call delay

        return new YouTubeVideoInfo
        {
            Id = "dQw4w9WgXcQ",
            Title = "Mock Video - Sample YouTube Content",
            Description = "This is a mock video description for testing purposes.",
            ThumbnailUrl = "https://img.youtube.com/vi/dQw4w9WgXcQ/maxresdefault.jpg",
            Duration = TimeSpan.FromMinutes(3).Add(TimeSpan.FromSeconds(33)),
            ViewCount = 1234567890,
            LikeCount = 12345678,
            ChannelName = "Mock Channel",
            UploadDate = DateTime.UtcNow.AddDays(-30),
            Tags = new List<string> { "mock", "testing", "youtube", "sample" }
        };
    }

    public async Task<string> DownloadVideoAsync(string url, string outputPath)
    {
        _logger.LogInformation("Mock: Downloading video from URL: {Url} to {Path}", url, outputPath);

        await Task.Delay(2000); // Simulate download time

        Directory.CreateDirectory(outputPath);
        var fileName = "mock_video.mp4";
        var fullPath = Path.Combine(outputPath, fileName);

        // Create a dummy file
        await File.WriteAllTextAsync(fullPath, "Mock video file content");

        return fullPath;
    }

    public async Task<string> DownloadAudioAsync(string url, string outputPath)
    {
        _logger.LogInformation("Mock: Downloading audio from URL: {Url} to {Path}", url, outputPath);

        await Task.Delay(1500); // Simulate download time

        Directory.CreateDirectory(outputPath);
        var fileName = "mock_audio.mp3";
        var fullPath = Path.Combine(outputPath, fileName);

        // Create a dummy file
        await File.WriteAllTextAsync(fullPath, "Mock audio file content");

        return fullPath;
    }

    public async Task<bool> IsValidYouTubeUrlAsync(string url)
    {
        await Task.Delay(100);

        // Simple mock validation - accept URLs with youtube.com or youtu.be
        return url.Contains("youtube.com") || url.Contains("youtu.be") || url.Length == 11;
    }

    public string ExtractVideoIdFromUrl(string url)
    {
        _logger.LogDebug("Mock: Extracting video ID from URL: {Url}", url);

        // Return mock video ID for testing
        return "dQw4w9WgXcQ";
    }
}