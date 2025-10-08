using FluentValidation;
using YoutubeRag.Application.DTOs.Video;

namespace YoutubeRag.Application.Validators.Video;

public class VideoUrlRequestValidator : AbstractValidator<VideoUrlRequest>
{
    public VideoUrlRequestValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("URL is required")
            .Must(BeValidYouTubeUrl).WithMessage("Must be a valid YouTube URL")
            .MaximumLength(500).WithMessage("URL is too long");

        RuleFor(x => x.Title)
            .MaximumLength(255).WithMessage("Title cannot exceed 255 characters")
            .When(x => !string.IsNullOrEmpty(x.Title));

        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage("Description cannot exceed 5000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }

    private bool BeValidYouTubeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        // Support multiple YouTube URL formats
        return url.Contains("youtube.com/watch?v=") ||
               url.Contains("youtu.be/") ||
               url.Contains("youtube.com/embed/") ||
               url.Contains("youtube.com/v/");
    }
}