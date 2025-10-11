using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using YoutubeRag.Application.Interfaces;

namespace YoutubeRag.Infrastructure.Services;

public class YouTubeService : IYouTubeService
{
    private readonly YoutubeClient _youtube;
    private readonly ILogger<YouTubeService> _logger;
    private readonly string _downloadBasePath;

    public YouTubeService(ILogger<YouTubeService> logger)
    {
        _youtube = new YoutubeClient();
        _logger = logger;
        _downloadBasePath = Path.Combine(Path.GetTempPath(), "YoutubeRag", "Downloads");
        Directory.CreateDirectory(_downloadBasePath);
    }

    public async Task<YouTubeVideoInfo> GetVideoInfoAsync(string url)
    {
        try
        {
            var videoId = ExtractVideoIdFromUrl(url);
            var video = await _youtube.Videos.GetAsync(videoId);

            return new YouTubeVideoInfo
            {
                Id = video.Id,
                Title = video.Title,
                Description = video.Description,
                ThumbnailUrl = video.Thumbnails.OrderByDescending(t => t.Resolution.Area).FirstOrDefault()?.Url ?? "",
                Duration = video.Duration ?? TimeSpan.Zero,
                ViewCount = (int)(video.Engagement.ViewCount),
                LikeCount = (int)video.Engagement.LikeCount,
                ChannelName = video.Author.ChannelTitle,
                UploadDate = video.UploadDate.DateTime,
                Tags = video.Keywords.ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting video info for URL: {Url}", url);
            throw;
        }
    }

    public async Task<string> DownloadVideoAsync(string url, string outputPath)
    {
        try
        {
            var videoId = ExtractVideoIdFromUrl(url);
            var streamManifest = await _youtube.Videos.Streams.GetManifestAsync(videoId);

            var videoStreamInfo = streamManifest.GetMuxedStreams()
                .Where(s => s.Container == Container.Mp4)
                .GetWithHighestVideoQuality();

            if (videoStreamInfo == null)
            {
                // If no muxed stream, get video-only stream
                videoStreamInfo = streamManifest.GetVideoOnlyStreams()
                    .Where(s => s.Container == Container.Mp4)
                    .GetWithHighestVideoQuality();
            }

            if (videoStreamInfo == null)
            {
                throw new InvalidOperationException("No suitable video stream found");
            }

            var fileName = $"{videoId}.{videoStreamInfo.Container.Name}";
            var fullPath = Path.Combine(outputPath, fileName);
            Directory.CreateDirectory(outputPath);

            await _youtube.Videos.Streams.DownloadAsync(videoStreamInfo, fullPath);

            _logger.LogInformation("Video downloaded successfully: {FilePath}", fullPath);
            return fullPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading video from URL: {Url}", url);
            throw;
        }
    }

    public async Task<string> DownloadAudioAsync(string url, string outputPath)
    {
        try
        {
            var videoId = ExtractVideoIdFromUrl(url);
            var streamManifest = await _youtube.Videos.Streams.GetManifestAsync(videoId);

            var audioStreamInfo = streamManifest.GetAudioOnlyStreams()
                .Where(s => s.Container == Container.Mp4 || s.Container == Container.WebM)
                .GetWithHighestBitrate();

            if (audioStreamInfo == null)
            {
                throw new InvalidOperationException("No suitable audio stream found");
            }

            var fileName = $"{videoId}_audio.{audioStreamInfo.Container.Name}";
            var fullPath = Path.Combine(outputPath, fileName);
            Directory.CreateDirectory(outputPath);

            await _youtube.Videos.Streams.DownloadAsync(audioStreamInfo, fullPath);

            _logger.LogInformation("Audio downloaded successfully: {FilePath}", fullPath);
            return fullPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading audio from URL: {Url}", url);
            throw;
        }
    }

    public async Task<bool> IsValidYouTubeUrlAsync(string url)
    {
        try
        {
            var videoId = ExtractVideoIdFromUrl(url);
            if (string.IsNullOrEmpty(videoId))
            {
                return false;
            }

            await _youtube.Videos.GetAsync(videoId);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string ExtractVideoIdFromUrl(string url)
    {
        var patterns = new[]
        {
            @"(?:youtube\.com\/watch\?v=|youtu\.be\/|youtube\.com\/embed\/)([a-zA-Z0-9_-]{11})",
            @"(?:youtube\.com\/v\/)([a-zA-Z0-9_-]{11})",
            @"^([a-zA-Z0-9_-]{11})$" // Direct video ID
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(url, pattern);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        throw new ArgumentException($"Invalid YouTube URL: {url}");
    }
}
