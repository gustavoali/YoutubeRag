using FluentValidation;
using YoutubeRag.Application.DTOs.Auth;

namespace YoutubeRag.Application.Validators.Auth;

/// <summary>
/// Validator for reset password request DTO
/// </summary>
public class ResetPasswordRequestDtoValidator : AbstractValidator<ResetPasswordRequestDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResetPasswordRequestDtoValidator"/> class
    /// </summary>
    public ResetPasswordRequestDtoValidator()
    {
        RuleFor(x => x.Token)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Reset token is required")
            .MinimumLength(32).WithMessage("Invalid reset token format");

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters");

        RuleFor(x => x.NewPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .MaximumLength(100).WithMessage("Password cannot exceed 100 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit")
            .Matches(@"[!@#$%^&*(),.?""':{}|<>]").WithMessage("Password must contain at least one special character")
            .Must(NotContainCommonPatterns).WithMessage("Password is too common or contains predictable patterns");

        RuleFor(x => x.ConfirmPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Password confirmation is required")
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match");
    }

    /// <summary>
    /// Checks if password contains common patterns that should be avoided
    /// </summary>
    private bool NotContainCommonPatterns(string password)
    {
        if (string.IsNullOrEmpty(password)) return false;

        var commonPatterns = new[]
        {
            "password", "123456", "qwerty", "admin", "letmein",
            "welcome", "monkey", "dragon", "master", "abc123"
        };

        var lowerPassword = password.ToLowerInvariant();
        return !commonPatterns.Any(pattern => lowerPassword.Contains(pattern));
    }
}