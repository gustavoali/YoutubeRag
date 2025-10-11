using System.Text.Json;
using FluentValidation;
using YoutubeRag.Application.DTOs.Video;

namespace YoutubeRag.Application.Validators.Video;

/// <summary>
/// Validator for create video DTO
/// </summary>
public class CreateVideoDtoValidator : AbstractValidator<CreateVideoDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateVideoDtoValidator"/> class
    /// </summary>
    public CreateVideoDtoValidator()
    {
        RuleFor(x => x.Title)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Title is required")
            .Length(1, 255).WithMessage("Title must be between 1 and 255 characters")
            .Matches(@"^[^<>]+$").WithMessage("Title cannot contain HTML tags");

        When(x => !string.IsNullOrEmpty(x.Description), () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(5000).WithMessage("Description cannot exceed 5000 characters")
                .Matches(@"^[^<>]*$").WithMessage("Description cannot contain HTML tags");
        });

        When(x => !string.IsNullOrEmpty(x.YoutubeUrl), () =>
        {
            RuleFor(x => x.YoutubeUrl)
                .MaximumLength(500).WithMessage("YouTube URL cannot exceed 500 characters")
                .Must(BeValidYoutubeUrl).WithMessage("Invalid YouTube URL format. URL must be from youtube.com or youtu.be");
        });

        When(x => !string.IsNullOrEmpty(x.ThumbnailUrl), () =>
        {
            RuleFor(x => x.ThumbnailUrl)
                .MaximumLength(500).WithMessage("Thumbnail URL cannot exceed 500 characters")
                .Must(BeValidUrl).WithMessage("Invalid thumbnail URL format")
                .Must(BeValidImageUrl).WithMessage("Thumbnail URL must point to an image file");
        });

        When(x => !string.IsNullOrEmpty(x.Metadata), () =>
        {
            RuleFor(x => x.Metadata)
                .Must(BeValidJson).WithMessage("Metadata must be valid JSON format")
                .MaximumLength(10000).WithMessage("Metadata cannot exceed 10000 characters");
        });

        When(x => !string.IsNullOrEmpty(x.Language), () =>
        {
            RuleFor(x => x.Language)
                .MaximumLength(10).WithMessage("Language code cannot exceed 10 characters")
                .Matches(@"^[a-z]{2}(-[A-Z]{2})?$").WithMessage("Invalid language code format. Use ISO 639-1 format (e.g., 'en' or 'en-US')");
        });

        // Ensure either YouTube URL or file upload would be provided (can't validate file upload here)
        RuleFor(x => x.YoutubeUrl)
            .NotEmpty().WithMessage("YouTube URL is required when not uploading a file")
            .When(x => string.IsNullOrEmpty(x.YoutubeUrl));
    }

    /// <summary>
    /// Validates that the URL is a valid YouTube URL
    /// </summary>
    private bool BeValidYoutubeUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return false;
        }

        try
        {
            var uri = new Uri(url);
            var host = uri.Host.ToLowerInvariant();

            // Check for valid YouTube domains
            if (!host.Contains("youtube.com") && !host.Contains("youtu.be") && !host.Contains("youtube-nocookie.com"))
            {
                return false;
            }

            // For youtube.com, check for video ID in query string
            if (host.Contains("youtube.com"))
            {
                var query = uri.Query;
                return query.Contains("v=") || uri.AbsolutePath.Contains("/embed/") || uri.AbsolutePath.Contains("/v/");
            }

            // For youtu.be, check for video ID in path
            if (host.Contains("youtu.be"))
            {
                return !string.IsNullOrEmpty(uri.AbsolutePath) && uri.AbsolutePath.Length > 1;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates that the string is a valid URL
    /// </summary>
    private bool BeValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return true;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Validates that the URL points to an image file
    /// </summary>
    private bool BeValidImageUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return true;
        }

        var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".bmp" };
        var urlLower = url.ToLowerInvariant();

        // Check if URL ends with a valid image extension or contains common image patterns
        return validExtensions.Any(ext => urlLower.Contains(ext)) ||
               urlLower.Contains("/img/") ||
               urlLower.Contains("/images/") ||
               urlLower.Contains("/thumbnail") ||
               urlLower.Contains("ytimg.com") || // YouTube thumbnails
               urlLower.Contains("ggpht.com"); // Google Photos
    }

    /// <summary>
    /// Validates that the string is valid JSON
    /// </summary>
    private bool BeValidJson(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return true;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
