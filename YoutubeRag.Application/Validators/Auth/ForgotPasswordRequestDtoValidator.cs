using FluentValidation;
using YoutubeRag.Application.DTOs.Auth;

namespace YoutubeRag.Application.Validators.Auth;

/// <summary>
/// Validator for forgot password request DTO
/// </summary>
public class ForgotPasswordRequestDtoValidator : AbstractValidator<ForgotPasswordRequestDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForgotPasswordRequestDtoValidator"/> class
    /// </summary>
    public ForgotPasswordRequestDtoValidator()
    {
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters");
    }
}