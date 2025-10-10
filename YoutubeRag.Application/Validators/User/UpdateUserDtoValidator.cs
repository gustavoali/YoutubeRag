using FluentValidation;
using YoutubeRag.Application.DTOs.User;

namespace YoutubeRag.Application.Validators.User;

/// <summary>
/// Validator for update user DTO
/// </summary>
public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateUserDtoValidator"/> class
    /// </summary>
    public UpdateUserDtoValidator()
    {
        // Only validate if the field is provided (all fields are optional in update)
        When(x => !string.IsNullOrEmpty(x.Name), () =>
        {
            RuleFor(x => x.Name)
                .Length(2, 100).WithMessage("Name must be between 2 and 100 characters")
                .Matches(@"^[a-zA-Z\s\-'\.]+$").WithMessage("Name can only contain letters, spaces, hyphens, apostrophes, and periods");
        });

        When(x => !string.IsNullOrEmpty(x.Bio), () =>
        {
            RuleFor(x => x.Bio)
                .MaximumLength(500).WithMessage("Bio cannot exceed 500 characters")
                .Matches(@"^[^<>]*$").WithMessage("Bio cannot contain HTML tags");
        });

        When(x => !string.IsNullOrEmpty(x.Avatar), () =>
        {
            RuleFor(x => x.Avatar)
                .MaximumLength(500).WithMessage("Avatar URL cannot exceed 500 characters")
                .Must(BeValidUrl).WithMessage("Invalid avatar URL format")
                .Must(BeValidImageUrl).WithMessage("Avatar URL must point to an image file");
        });

        // Ensure RemoveAvatar and Avatar are not both set
        RuleFor(x => x)
            .Must(x => !(x.RemoveAvatar == true && !string.IsNullOrEmpty(x.Avatar)))
            .WithMessage("Cannot both set an avatar and remove it at the same time")
            .WithName("Avatar");

        // Ensure at least one field is provided for update
        RuleFor(x => x)
            .Must(HaveAtLeastOneFieldToUpdate)
            .WithMessage("At least one field must be provided for update")
            .WithName("UpdateFields");
    }

    /// <summary>
    /// Validates that at least one field is provided for update
    /// </summary>
    private bool HaveAtLeastOneFieldToUpdate(UpdateUserDto dto)
    {
        return !string.IsNullOrEmpty(dto.Name) ||
               !string.IsNullOrEmpty(dto.Bio) ||
               !string.IsNullOrEmpty(dto.Avatar) ||
               dto.RemoveAvatar.HasValue ||
               dto.IsActive.HasValue;
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

        // Check if URL ends with a valid image extension
        return validExtensions.Any(ext => urlLower.Contains(ext)) ||
               // Or if it contains common image service patterns
               urlLower.Contains("gravatar.com") ||
               urlLower.Contains("avatars.githubusercontent.com") ||
               urlLower.Contains("cloudinary.com") ||
               urlLower.Contains("imgur.com");
    }
}