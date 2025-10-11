using FluentValidation;
using YoutubeRag.Application.DTOs.Auth;

namespace YoutubeRag.Application.Validators.Auth;

/// <summary>
/// Validator for email verification request DTO
/// </summary>
public class VerifyEmailRequestDtoValidator : AbstractValidator<VerifyEmailRequestDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VerifyEmailRequestDtoValidator"/> class
    /// </summary>
    public VerifyEmailRequestDtoValidator()
    {
        RuleFor(x => x.Token)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Verification token is required")
            .MinimumLength(32).WithMessage("Invalid verification token format")
            .Matches(@"^[A-Za-z0-9\-_]+$").WithMessage("Verification token contains invalid characters");

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters");
    }
}
