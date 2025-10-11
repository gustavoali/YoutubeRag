using FluentValidation;
using YoutubeRag.Application.DTOs.TranscriptSegment;

namespace YoutubeRag.Application.Validators.TranscriptSegment;

/// <summary>
/// Validator for update transcript segment DTO
/// </summary>
public class UpdateTranscriptSegmentDtoValidator : AbstractValidator<UpdateTranscriptSegmentDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateTranscriptSegmentDtoValidator"/> class
    /// </summary>
    public UpdateTranscriptSegmentDtoValidator()
    {
        When(x => !string.IsNullOrEmpty(x.Text), () =>
        {
            RuleFor(x => x.Text)
                .MinimumLength(1).WithMessage("Text cannot be empty")
                .MaximumLength(10000).WithMessage("Text cannot exceed 10000 characters")
                .Must(NotContainControlCharacters).WithMessage("Text contains invalid control characters");
        });

        When(x => x.StartTime.HasValue, () =>
        {
            RuleFor(x => x.StartTime)
                .GreaterThanOrEqualTo(0).WithMessage("Start time must be non-negative")
                .LessThan(86400).WithMessage("Start time cannot exceed 24 hours (86400 seconds)");
        });

        When(x => x.EndTime.HasValue, () =>
        {
            RuleFor(x => x.EndTime)
                .GreaterThanOrEqualTo(0).WithMessage("End time must be non-negative")
                .LessThan(86400).WithMessage("End time cannot exceed 24 hours (86400 seconds)");
        });

        // Validate that end time is after start time when both are provided
        When(x => x.StartTime.HasValue && x.EndTime.HasValue, () =>
        {
            RuleFor(x => x.EndTime)
                .GreaterThan(x => x.StartTime!.Value).WithMessage("End time must be after start time");
        });

        When(x => x.Confidence.HasValue, () =>
        {
            RuleFor(x => x.Confidence)
                .InclusiveBetween(0, 1).WithMessage("Confidence must be between 0 and 1");
        });

        When(x => !string.IsNullOrEmpty(x.Language), () =>
        {
            RuleFor(x => x.Language)
                .MaximumLength(10).WithMessage("Language code cannot exceed 10 characters")
                .Matches(@"^[a-z]{2}(-[A-Z]{2})?$").WithMessage("Invalid language code format. Use ISO 639-1 format (e.g., 'en' or 'en-US')");
        });

        // Validate time range is reasonable when both times are provided
        When(x => x.StartTime.HasValue && x.EndTime.HasValue, () =>
        {
            RuleFor(x => x)
                .Must(HaveReasonableTimeRange)
                .WithMessage("Segment duration is invalid (too short or too long)")
                .WithName("TimeRange");
        });

        // Ensure at least one field is provided for update
        RuleFor(x => x)
            .Must(HaveAtLeastOneFieldToUpdate)
            .WithMessage("At least one field must be provided for update")
            .WithName("UpdateFields");
    }

    /// <summary>
    /// Checks that text doesn't contain control characters (except common ones like newline)
    /// </summary>
    private bool NotContainControlCharacters(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return true;
        }

        foreach (char c in text)
        {
            // Allow common whitespace characters
            if (c == '\n' || c == '\r' || c == '\t')
            {
                continue;
            }

            // Reject other control characters
            if (char.IsControl(c))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Validates that the time range is reasonable
    /// </summary>
    private bool HaveReasonableTimeRange(UpdateTranscriptSegmentDto dto)
    {
        if (!dto.StartTime.HasValue || !dto.EndTime.HasValue)
        {
            return true;
        }

        var duration = dto.EndTime.Value - dto.StartTime.Value;

        // Segment should be at least 0.1 seconds
        if (duration < 0.1)
        {
            return false;
        }

        // Single segment shouldn't be longer than 5 minutes (300 seconds)
        if (duration > 300)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates that at least one field is provided for update
    /// </summary>
    private bool HaveAtLeastOneFieldToUpdate(UpdateTranscriptSegmentDto dto)
    {
        return !string.IsNullOrEmpty(dto.Text) ||
               dto.StartTime.HasValue ||
               dto.EndTime.HasValue ||
               dto.Confidence.HasValue ||
               !string.IsNullOrEmpty(dto.Language);
    }
}
