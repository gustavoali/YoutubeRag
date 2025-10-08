using FluentValidation;
using YoutubeRag.Application.DTOs.Auth;

namespace YoutubeRag.Application.Validators.Auth;

/// <summary>
/// Validator for refresh token request DTO
/// </summary>
public class RefreshTokenRequestDtoValidator : AbstractValidator<RefreshTokenRequestDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokenRequestDtoValidator"/> class
    /// </summary>
    public RefreshTokenRequestDtoValidator()
    {
        RuleFor(x => x.RefreshToken)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Refresh token is required")
            .MinimumLength(32).WithMessage("Invalid refresh token format")
            .Matches(@"^[A-Za-z0-9\-_]+$").WithMessage("Refresh token contains invalid characters");

        When(x => !string.IsNullOrEmpty(x.DeviceInfo), () =>
        {
            RuleFor(x => x.DeviceInfo)
                .MaximumLength(255).WithMessage("Device info cannot exceed 255 characters");
        });
    }
}