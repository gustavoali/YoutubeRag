using FluentValidation;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace YoutubeRag.Application.Validators.Common;

/// <summary>
/// Custom validation extensions for common scenarios
/// </summary>
public static class CustomValidationExtensions
{
    /// <summary>
    /// Validates that a string is a valid GUID
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeValidGuid<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Must(value => string.IsNullOrEmpty(value) || Guid.TryParse(value, out _))
            .WithMessage("{PropertyName} must be a valid GUID format");
    }

    /// <summary>
    /// Validates that a string is a valid GUID (required)
    /// </summary>
    public static IRuleBuilderOptions<T, string> MustBeValidGuidRequired<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("{PropertyName} is required")
            .Must(value => Guid.TryParse(value, out _))
            .WithMessage("{PropertyName} must be a valid GUID format");
    }

    /// <summary>
    /// Validates that a string is valid JSON
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeValidJson<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Must(value =>
            {
                if (string.IsNullOrEmpty(value)) return true;
                try
                {
                    using var doc = JsonDocument.Parse(value);
                    return true;
                }
                catch (JsonException)
                {
                    return false;
                }
            })
            .WithMessage("{PropertyName} must be valid JSON format");
    }

    /// <summary>
    /// Validates that a string is a valid URL
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeValidUrl<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Must(value =>
            {
                if (string.IsNullOrEmpty(value)) return true;
                return Uri.TryCreate(value, UriKind.Absolute, out var result) &&
                       (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
            })
            .WithMessage("{PropertyName} must be a valid HTTP or HTTPS URL");
    }

    /// <summary>
    /// Validates that a string is a valid email with optional domain restrictions
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeValidEmail<T>(
        this IRuleBuilder<T, string?> ruleBuilder,
        params string[] blockedDomains)
    {
        return ruleBuilder
            .EmailAddress().WithMessage("{PropertyName} must be a valid email address")
            .Must(email =>
            {
                if (string.IsNullOrEmpty(email) || blockedDomains.Length == 0) return true;
                var domain = email.Split('@').LastOrDefault()?.ToLowerInvariant();
                return !string.IsNullOrEmpty(domain) && !blockedDomains.Contains(domain);
            })
            .WithMessage("{PropertyName} domain is not allowed");
    }

    /// <summary>
    /// Validates that a string is a valid YouTube URL
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeValidYouTubeUrl<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Must(value =>
            {
                if (string.IsNullOrEmpty(value)) return true;
                try
                {
                    var uri = new Uri(value);
                    var host = uri.Host.ToLowerInvariant();

                    if (!host.Contains("youtube.com") && !host.Contains("youtu.be") && !host.Contains("youtube-nocookie.com"))
                        return false;

                    if (host.Contains("youtube.com"))
                        return uri.Query.Contains("v=") || uri.AbsolutePath.Contains("/embed/") || uri.AbsolutePath.Contains("/v/");

                    if (host.Contains("youtu.be"))
                        return !string.IsNullOrEmpty(uri.AbsolutePath) && uri.AbsolutePath.Length > 1;

                    return true;
                }
                catch
                {
                    return false;
                }
            })
            .WithMessage("{PropertyName} must be a valid YouTube URL");
    }

    /// <summary>
    /// Validates that a string is a valid language code (ISO 639-1)
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeValidLanguageCode<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Matches(@"^[a-z]{2}(-[A-Z]{2})?$")
            .WithMessage("{PropertyName} must be a valid ISO 639-1 language code (e.g., 'en' or 'en-US')");
    }

    /// <summary>
    /// Validates that a string does not contain HTML tags
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustNotContainHtml<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Must(value =>
            {
                if (string.IsNullOrEmpty(value)) return true;
                return !Regex.IsMatch(value, @"<[^>]+>");
            })
            .WithMessage("{PropertyName} cannot contain HTML tags");
    }

    /// <summary>
    /// Validates that a string is a strong password
    /// </summary>
    public static IRuleBuilderOptions<T, string> MustBeStrongPassword<T>(
        this IRuleBuilder<T, string> ruleBuilder,
        int minLength = 8)
    {
        return ruleBuilder
            .MinimumLength(minLength).WithMessage($"{{PropertyName}} must be at least {minLength} characters")
            .Matches(@"[A-Z]").WithMessage("{PropertyName} must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("{PropertyName} must contain at least one lowercase letter")
            .Matches(@"[0-9]").WithMessage("{PropertyName} must contain at least one digit")
            .Matches(@"[!@#$%^&*(),.?""':{}|<>]").WithMessage("{PropertyName} must contain at least one special character");
    }

    /// <summary>
    /// Validates that a string is a valid base64 encoded string
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeValidBase64<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Must(value =>
            {
                if (string.IsNullOrEmpty(value)) return true;
                try
                {
                    Convert.FromBase64String(value);
                    return true;
                }
                catch
                {
                    return false;
                }
            })
            .WithMessage("{PropertyName} must be a valid Base64 encoded string");
    }

    /// <summary>
    /// Validates that a string is a valid phone number (basic validation)
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeValidPhoneNumber<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Matches(@"^[\+]?[(]?[0-9]{1,4}[)]?[-\s\.]?[(]?[0-9]{1,4}[)]?[-\s\.]?[0-9]{1,9}$")
            .WithMessage("{PropertyName} must be a valid phone number");
    }

    /// <summary>
    /// Validates file size in bytes
    /// </summary>
    public static IRuleBuilderOptions<T, long> MustBeValidFileSize<T>(
        this IRuleBuilder<T, long> ruleBuilder,
        long maxSizeInBytes)
    {
        return ruleBuilder
            .InclusiveBetween(1, maxSizeInBytes)
            .WithMessage($"{{PropertyName}} must be between 1 byte and {FormatFileSize(maxSizeInBytes)}");
    }

    /// <summary>
    /// Validates that a string matches a specific date format
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeValidDateFormat<T>(
        this IRuleBuilder<T, string?> ruleBuilder,
        string format = "yyyy-MM-dd")
    {
        return ruleBuilder
            .Must(value =>
            {
                if (string.IsNullOrEmpty(value)) return true;
                return DateTime.TryParseExact(value, format, null,
                    System.Globalization.DateTimeStyles.None, out _);
            })
            .WithMessage($"{{PropertyName}} must be in {format} format");
    }

    /// <summary>
    /// Validates that a DateTime is not in the future
    /// </summary>
    public static IRuleBuilderOptions<T, DateTime> MustNotBeFutureDate<T>(this IRuleBuilder<T, DateTime> ruleBuilder)
    {
        return ruleBuilder
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("{PropertyName} cannot be a future date");
    }

    /// <summary>
    /// Validates that a DateTime is not in the past
    /// </summary>
    public static IRuleBuilderOptions<T, DateTime> MustNotBePastDate<T>(this IRuleBuilder<T, DateTime> ruleBuilder)
    {
        return ruleBuilder
            .GreaterThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("{PropertyName} cannot be a past date");
    }

    /// <summary>
    /// Helper method to format file size
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}