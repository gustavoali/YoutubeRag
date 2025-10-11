using FluentValidation;
using YoutubeRag.Application.DTOs.User;

namespace YoutubeRag.Application.Validators.User;

/// <summary>
/// Validator for create user DTO
/// </summary>
public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateUserDtoValidator"/> class
    /// </summary>
    public CreateUserDtoValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Name is required")
            .Length(2, 100).WithMessage("Name must be between 2 and 100 characters")
            .Matches(@"^[a-zA-Z\s\-'\.]+$").WithMessage("Name can only contain letters, spaces, hyphens, apostrophes, and periods");

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters")
            .Must(BeValidEmailDomain).WithMessage("Email domain appears to be invalid");

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Password is required")
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
            .Equal(x => x.Password).WithMessage("Passwords do not match");

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
                .Must(BeValidUrl).WithMessage("Invalid avatar URL format");
        });
    }

    /// <summary>
    /// Validates that the email domain is not from a disposable email service
    /// </summary>
    private bool BeValidEmailDomain(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return false;
        }

        // List of common disposable email domains to block
        var disposableDomains = new[]
        {
            "tempmail.com", "throwaway.email", "guerrillamail.com",
            "mailinator.com", "10minutemail.com", "trash-mail.com"
        };

        var domain = email.Split('@').LastOrDefault()?.ToLowerInvariant();
        return !string.IsNullOrEmpty(domain) && !disposableDomains.Contains(domain);
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

    /// <summary>
    /// Validates that the string is a valid URL
    /// </summary>
    private bool BeValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return true;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}
