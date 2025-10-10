using FluentValidation;
using YoutubeRag.Application.DTOs.Video;
using System.Text.Json;

namespace YoutubeRag.Application.Validators.Video;

/// <summary>
/// Validator for update video DTO
/// </summary>
public class UpdateVideoDtoValidator : AbstractValidator<UpdateVideoDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateVideoDtoValidator"/> class
    /// </summary>
    public UpdateVideoDtoValidator()
    {
        When(x => !string.IsNullOrEmpty(x.Title), () =>
        {
            RuleFor(x => x.Title)
                .Length(1, 255).WithMessage("Title must be between 1 and 255 characters")
                .Matches(@"^[^<>]+$").WithMessage("Title cannot contain HTML tags");
        });

        When(x => !string.IsNullOrEmpty(x.Description), () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(5000).WithMessage("Description cannot exceed 5000 characters")
                .Matches(@"^[^<>]*$").WithMessage("Description cannot contain HTML tags");
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

        // Ensure conflicting operations are not requested
        RuleFor(x => x)
            .Must(x => !(x.ClearDescription == true && !string.IsNullOrEmpty(x.Description)))
            .WithMessage("Cannot both set and clear description at the same time")
            .WithName("Description");

        RuleFor(x => x)
            .Must(x => !(x.ClearThumbnail == true && !string.IsNullOrEmpty(x.ThumbnailUrl)))
            .WithMessage("Cannot both set and clear thumbnail at the same time")
            .WithName("Thumbnail");

        // Ensure at least one field is provided for update
        RuleFor(x => x)
            .Must(HaveAtLeastOneFieldToUpdate)
            .WithMessage("At least one field must be provided for update")
            .WithName("UpdateFields");
    }

    /// <summary>
    /// Validates that at least one field is provided for update
    /// </summary>
    private bool HaveAtLeastOneFieldToUpdate(UpdateVideoDto dto)
    {
        return !string.IsNullOrEmpty(dto.Title) ||
               !string.IsNullOrEmpty(dto.Description) ||
               !string.IsNullOrEmpty(dto.ThumbnailUrl) ||
               !string.IsNullOrEmpty(dto.Metadata) ||
               dto.ClearDescription.HasValue ||
               dto.ClearThumbnail.HasValue;
    }

    /// <summary>
    /// Validates that the string is a valid URL
    /// </summary>
    private bool BeValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Validates that the URL points to an image file
    /// </summary>
    private bool BeValidImageUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return true;

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
        if (string.IsNullOrEmpty(json)) return true;

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