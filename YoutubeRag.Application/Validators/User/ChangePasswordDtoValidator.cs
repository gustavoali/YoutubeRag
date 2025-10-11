using FluentValidation;
using YoutubeRag.Application.DTOs.User;

namespace YoutubeRag.Application.Validators.User;

/// <summary>
/// Validator for change password DTO
/// </summary>
public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChangePasswordDtoValidator"/> class
    /// </summary>
    public ChangePasswordDtoValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Current password is required")
            .MinimumLength(1).WithMessage("Current password cannot be empty");

        RuleFor(x => x.NewPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .MaximumLength(100).WithMessage("Password cannot exceed 100 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit")
            .Matches(@"[!@#$%^&*(),.?""':{}|<>]").WithMessage("Password must contain at least one special character")
            .Must(NotContainCommonPatterns).WithMessage("Password is too common or contains predictable patterns")
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must be different from current password");

        RuleFor(x => x.ConfirmNewPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Password confirmation is required")
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match");
    }

    /// <summary>
    /// Checks if password contains common patterns that should be avoided
    /// </summary>
    private bool NotContainCommonPatterns(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return false;
        }

        var commonPatterns = new[]
        {
            "password", "123456", "qwerty", "admin", "letmein",
            "welcome", "monkey", "dragon", "master", "abc123"
        };

        var lowerPassword = password.ToLowerInvariant();
        return !commonPatterns.Any(pattern => lowerPassword.Contains(pattern));
    }
}
