using FluentValidation;
using YoutubeRag.Application.DTOs.Auth;

namespace YoutubeRag.Application.Validators.Auth;

/// <summary>
/// Validator for Google OAuth authentication request DTO
/// </summary>
public class GoogleAuthRequestDtoValidator : AbstractValidator<GoogleAuthRequestDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleAuthRequestDtoValidator"/> class
    /// </summary>
    public GoogleAuthRequestDtoValidator()
    {
        RuleFor(x => x.GoogleToken)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Google token is required")
            .MinimumLength(100).WithMessage("Invalid Google token format")
            .Must(BeValidJwtFormat).WithMessage("Google token must be a valid JWT token");

        When(x => !string.IsNullOrEmpty(x.DeviceInfo), () =>
        {
            RuleFor(x => x.DeviceInfo)
                .MaximumLength(255).WithMessage("Device info cannot exceed 255 characters");
        });
    }

    /// <summary>
    /// Validates that the token has a valid JWT format
    /// </summary>
    private bool BeValidJwtFormat(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        // JWT tokens have three parts separated by dots
        var parts = token.Split('.');
        return parts.Length == 3 && parts.All(part => !string.IsNullOrEmpty(part));
    }
}
