using FluentValidation;
using YoutubeRag.Application.DTOs.Video;

namespace YoutubeRag.Application.Validators.Video;

/// <summary>
/// Validator for VideoMetadataDto ensuring all business rules are met (YRUS-0102 AC3)
/// </summary>
public class VideoMetadataValidator : AbstractValidator<VideoMetadataDto>
{
    private const int MinDurationSeconds = 1;
    private const int MaxDurationSeconds = 14400; // 4 hours
    private const int MaxTitleLength = 500;

    public VideoMetadataValidator()
    {
        // AC3: Título no vacío
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Video title is required")
            .WithErrorCode("INVALID_TITLE");

        RuleFor(x => x.Title)
            .MaximumLength(MaxTitleLength)
            .WithMessage($"Video title cannot exceed {MaxTitleLength} characters")
            .WithErrorCode("TITLE_TOO_LONG")
            .When(x => !string.IsNullOrWhiteSpace(x.Title));

        // AC3: Duración > 0 y < 14400 segundos (4 horas max)
        RuleFor(x => x.Duration)
            .NotNull()
            .WithMessage("Video duration is required")
            .WithErrorCode("INVALID_DURATION");

        RuleFor(x => x.DurationSeconds)
            .GreaterThanOrEqualTo(MinDurationSeconds)
            .WithMessage($"Video duration must be at least {MinDurationSeconds} second")
            .WithErrorCode("INVALID_DURATION")
            .When(x => x.Duration.HasValue);

        RuleFor(x => x.DurationSeconds)
            .LessThanOrEqualTo(MaxDurationSeconds)
            .WithMessage($"Video duration cannot exceed {MaxDurationSeconds / 3600} hours (video is {{PropertyValue}} seconds)")
            .WithErrorCode("VIDEO_TOO_LONG")
            .When(x => x.Duration.HasValue);

        // AC3: Thumbnail URL válida
        RuleFor(x => x.ThumbnailUrl)
            .NotEmpty()
            .WithMessage("Video thumbnail URL is required")
            .WithErrorCode("INVALID_THUMBNAIL");

        RuleFor(x => x.ThumbnailUrl)
            .Must(BeAValidUrl)
            .WithMessage("Video thumbnail URL must be a valid URL")
            .WithErrorCode("INVALID_THUMBNAIL_URL")
            .When(x => !string.IsNullOrWhiteSpace(x.ThumbnailUrl));

        // AC3: Warning si faltan campos opcionales (handled via warnings, not validation errors)
        // ViewCount, Description, Tags, CategoryId are optional but logged as warnings in the service

        // Campos requeridos adicionales
        RuleFor(x => x.ChannelTitle)
            .NotEmpty()
            .WithMessage("Channel title is required")
            .WithErrorCode("INVALID_CHANNEL");

        RuleFor(x => x.PublishedAt)
            .NotNull()
            .WithMessage("Published date is required")
            .WithErrorCode("INVALID_PUBLISHED_DATE");

        RuleFor(x => x.PublishedAt)
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1)) // Allow 1 day tolerance for timezone issues
            .WithMessage("Published date cannot be in the future")
            .WithErrorCode("INVALID_PUBLISHED_DATE")
            .When(x => x.PublishedAt.HasValue);
    }

    /// <summary>
    /// Validates that a string is a valid URL
    /// </summary>
    private bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
