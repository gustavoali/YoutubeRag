using FluentValidation;
using YoutubeRag.Application.DTOs.Common;

namespace YoutubeRag.Application.Validators.Common;

/// <summary>
/// Validator for pagination request DTO
/// </summary>
public class PaginationRequestDtoValidator : AbstractValidator<PaginationRequestDto>
{
    private readonly string[] _validSortFields = new[]
    {
        "id",
        "name",
        "title",
        "createdAt",
        "updatedAt",
        "email",
        "status",
        "priority",
        "startTime",
        "endTime",
        "duration",
        "viewCount"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="PaginationRequestDtoValidator"/> class
    /// </summary>
    public PaginationRequestDtoValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("Page number must be greater than 0")
            .LessThanOrEqualTo(10000).WithMessage("Page number cannot exceed 10000");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100");

        When(x => !string.IsNullOrEmpty(x.SortBy), () =>
        {
            RuleFor(x => x.SortBy)
                .MaximumLength(50).WithMessage("Sort field name cannot exceed 50 characters")
                .Matches(@"^[a-zA-Z][a-zA-Z0-9_]*$").WithMessage("Sort field name contains invalid characters")
                .Must(BeValidSortField).WithMessage($"Invalid sort field. Valid fields are: {string.Join(", ", _validSortFields)}");
        });

        When(x => !string.IsNullOrEmpty(x.SearchQuery), () =>
        {
            RuleFor(x => x.SearchQuery)
                .MaximumLength(200).WithMessage("Search query cannot exceed 200 characters")
                .MinimumLength(2).WithMessage("Search query must be at least 2 characters")
                .Must(NotContainDangerousCharacters).WithMessage("Search query contains potentially dangerous characters");
        });

        // Validate that the calculated skip value doesn't overflow
        RuleFor(x => x)
            .Must(HaveValidSkipValue)
            .WithMessage("Page number and page size combination results in invalid skip value")
            .WithName("Pagination");
    }

    /// <summary>
    /// Validates that the sort field is one of the allowed values
    /// </summary>
    private bool BeValidSortField(string? sortBy)
    {
        if (string.IsNullOrEmpty(sortBy))
        {
            return true;
        }

        return _validSortFields.Contains(sortBy.ToLowerInvariant());
    }

    /// <summary>
    /// Checks that search query doesn't contain SQL injection or XSS attempts
    /// </summary>
    private bool NotContainDangerousCharacters(string? query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return true;
        }

        // Check for common SQL injection patterns
        var dangerousPatterns = new[]
        {
            "--", "/*", "*/", "xp_", "sp_", "@@",
            "drop ", "insert ", "delete ", "update ",
            "exec ", "execute ", "script", "javascript:",
            "<script", "onclick", "onerror"
        };

        var lowerQuery = query.ToLowerInvariant();
        return !dangerousPatterns.Any(pattern => lowerQuery.Contains(pattern));
    }

    /// <summary>
    /// Validates that the skip value doesn't overflow
    /// </summary>
    private bool HaveValidSkipValue(PaginationRequestDto dto)
    {
        try
        {
            var skip = (dto.PageNumber - 1) * dto.PageSize;
            return skip >= 0 && skip < int.MaxValue - dto.PageSize;
        }
        catch
        {
            return false;
        }
    }
}
