using FluentValidation;
using YoutubeRag.Application.DTOs.Video;
using YoutubeRag.Application.Utilities;

namespace YoutubeRag.Application.Validators.Video;

/// <summary>
/// Validator for VideoUrlRequest DTOs
/// Ensures YouTube URLs are valid and extractable
/// </summary>
public class VideoUrlRequestValidator : AbstractValidator<VideoUrlRequest>
{
    public VideoUrlRequestValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty()
            .WithMessage("URL is required")
            .WithErrorCode("URL_REQUIRED");

        RuleFor(x => x.Url)
            .MaximumLength(2048)
            .WithMessage("URL cannot exceed 2048 characters")
            .WithErrorCode("URL_TOO_LONG")
            .When(x => !string.IsNullOrWhiteSpace(x.Url));

        RuleFor(x => x.Url)
            .Must(BeValidYouTubeUrl)
            .WithMessage("Must be a valid YouTube URL. Supported formats: youtube.com/watch?v=VIDEO_ID, youtu.be/VIDEO_ID, youtube.com/embed/VIDEO_ID, youtube.com/v/VIDEO_ID, youtube.com/shorts/VIDEO_ID")
            .WithErrorCode("INVALID_YOUTUBE_URL")
            .When(x => !string.IsNullOrWhiteSpace(x.Url));

        RuleFor(x => x.Url)
            .Must(HaveExtractableVideoId)
            .WithMessage("Could not extract a valid YouTube video ID from the URL. Video ID must be 11 characters (alphanumeric, hyphens, and underscores)")
            .WithErrorCode("INVALID_VIDEO_ID")
            .When(x => !string.IsNullOrWhiteSpace(x.Url) && BeValidYouTubeUrl(x.Url));

        RuleFor(x => x.Title)
            .MaximumLength(255)
            .WithMessage("Title cannot exceed 255 characters")
            .WithErrorCode("TITLE_TOO_LONG")
            .When(x => !string.IsNullOrEmpty(x.Title));

        RuleFor(x => x.Description)
            .MaximumLength(5000)
            .WithMessage("Description cannot exceed 5000 characters")
            .WithErrorCode("DESCRIPTION_TOO_LONG")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Priority)
            .IsInEnum()
            .WithMessage("Invalid priority value. Must be Low (0), Normal (1), High (2), or Critical (3)")
            .WithErrorCode("INVALID_PRIORITY");
    }

    /// <summary>
    /// Validates if the URL is a YouTube URL
    /// </summary>
    private bool BeValidYouTubeUrl(string? url)
    {
        return YouTubeUrlParser.IsYouTubeUrl(url);
    }

    /// <summary>
    /// Validates if a valid video ID can be extracted from the URL
    /// </summary>
    private bool HaveExtractableVideoId(string? url)
    {
        return YouTubeUrlParser.IsValidYouTubeUrl(url, out _);
    }
}
