using FluentValidation;
using YoutubeRag.Application.DTOs.Auth;

namespace YoutubeRag.Application.Validators.Auth;

/// <summary>
/// Validator for login request DTO
/// </summary>
public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginRequestDtoValidator"/> class
    /// </summary>
    public LoginRequestDtoValidator()
    {
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(1).WithMessage("Password cannot be empty");

        When(x => !string.IsNullOrEmpty(x.DeviceInfo), () =>
        {
            RuleFor(x => x.DeviceInfo)
                .MaximumLength(255).WithMessage("Device info cannot exceed 255 characters");
        });
    }
}