using System.Text.RegularExpressions;

namespace YoutubeRag.Application.Utilities;

/// <summary>
/// Utility class for parsing and validating YouTube URLs
/// </summary>
public static class YouTubeUrlParser
{
    // Comprehensive regex pattern to match YouTube URLs in various formats
    // Supports: youtube.com/watch?v=, youtu.be/, youtube.com/embed/, youtube.com/v/, youtube.com/shorts/
    private static readonly Regex YouTubeUrlRegex = new(
        @"^(?:https?:\/\/)?(?:www\.)?(?:youtube\.com\/(?:watch\?v=|embed\/|v\/|shorts\/)|youtu\.be\/)([a-zA-Z0-9_-]{11})(?:[&?].*)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    // Alternative regex for extracting video ID from any position in the URL
    private static readonly Regex YouTubeIdExtractorRegex = new(
        @"(?:youtube\.com\/(?:watch\?.*v=|embed\/|v\/|shorts\/)|youtu\.be\/)([a-zA-Z0-9_-]{11})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    /// <summary>
    /// Validates if a URL is a valid YouTube URL and extracts the video ID
    /// </summary>
    /// <param name="url">The URL to validate</param>
    /// <param name="videoId">The extracted video ID if valid, null otherwise</param>
    /// <returns>True if the URL is valid, false otherwise</returns>
    public static bool IsValidYouTubeUrl(string? url, out string? videoId)
    {
        videoId = null;

        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        // Try to match with the strict regex first
        var match = YouTubeUrlRegex.Match(url.Trim());
        if (match.Success)
        {
            videoId = match.Groups[1].Value;
            return ValidateVideoId(videoId);
        }

        // Fallback: Try to extract video ID from a more lenient pattern
        match = YouTubeIdExtractorRegex.Match(url.Trim());
        if (match.Success)
        {
            videoId = match.Groups[1].Value;
            return ValidateVideoId(videoId);
        }

        return false;
    }

    /// <summary>
    /// Extracts YouTube video ID from a URL
    /// </summary>
    /// <param name="url">The URL to parse</param>
    /// <returns>The video ID if found, null otherwise</returns>
    public static string? ExtractVideoId(string? url)
    {
        IsValidYouTubeUrl(url, out string? videoId);
        return videoId;
    }

    /// <summary>
    /// Validates that a video ID conforms to YouTube's format
    /// </summary>
    /// <param name="videoId">The video ID to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool ValidateVideoId(string? videoId)
    {
        if (string.IsNullOrWhiteSpace(videoId))
        {
            return false;
        }

        // YouTube video IDs are exactly 11 characters long
        // and consist of alphanumeric characters, hyphens, and underscores
        if (videoId.Length != 11)
        {
            return false;
        }

        // Validate characters (a-z, A-Z, 0-9, -, _)
        return Regex.IsMatch(videoId, @"^[a-zA-Z0-9_-]{11}$");
    }

    /// <summary>
    /// Checks if a URL is a YouTube URL (without strict validation)
    /// </summary>
    /// <param name="url">The URL to check</param>
    /// <returns>True if it appears to be a YouTube URL, false otherwise</returns>
    public static bool IsYouTubeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var normalized = url.Trim().ToLowerInvariant();
        return normalized.Contains("youtube.com") || normalized.Contains("youtu.be");
    }

    /// <summary>
    /// Normalizes a YouTube URL to the standard watch format
    /// </summary>
    /// <param name="url">The URL to normalize</param>
    /// <returns>Normalized URL in the format https://www.youtube.com/watch?v=VIDEO_ID, or null if invalid</returns>
    public static string? NormalizeUrl(string? url)
    {
        if (!IsValidYouTubeUrl(url, out string? videoId) || string.IsNullOrEmpty(videoId))
        {
            return null;
        }

        return $"https://www.youtube.com/watch?v={videoId}";
    }

    /// <summary>
    /// Gets detailed validation result with error message
    /// </summary>
    /// <param name="url">The URL to validate</param>
    /// <returns>Validation result with video ID and error message</returns>
    public static YouTubeUrlValidationResult Validate(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return new YouTubeUrlValidationResult
            {
                IsValid = false,
                VideoId = null,
                ErrorMessage = "URL cannot be empty"
            };
        }

        if (!IsYouTubeUrl(url))
        {
            return new YouTubeUrlValidationResult
            {
                IsValid = false,
                VideoId = null,
                ErrorMessage = "URL must be a valid YouTube URL (youtube.com or youtu.be)"
            };
        }

        if (!IsValidYouTubeUrl(url, out string? videoId))
        {
            return new YouTubeUrlValidationResult
            {
                IsValid = false,
                VideoId = null,
                ErrorMessage = "Could not extract a valid YouTube video ID from the URL"
            };
        }

        return new YouTubeUrlValidationResult
        {
            IsValid = true,
            VideoId = videoId,
            ErrorMessage = null,
            NormalizedUrl = NormalizeUrl(url)
        };
    }
}

/// <summary>
/// Result of YouTube URL validation
/// </summary>
public class YouTubeUrlValidationResult
{
    /// <summary>
    /// Indicates if the URL is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// The extracted video ID (if valid)
    /// </summary>
    public string? VideoId { get; set; }

    /// <summary>
    /// Error message if validation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Normalized URL in standard format
    /// </summary>
    public string? NormalizedUrl { get; set; }
}
