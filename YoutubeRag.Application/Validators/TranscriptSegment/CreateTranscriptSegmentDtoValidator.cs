using FluentValidation;
using YoutubeRag.Application.DTOs.TranscriptSegment;

namespace YoutubeRag.Application.Validators.TranscriptSegment;

/// <summary>
/// Validator for create transcript segment DTO
/// </summary>
public class CreateTranscriptSegmentDtoValidator : AbstractValidator<CreateTranscriptSegmentDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateTranscriptSegmentDtoValidator"/> class
    /// </summary>
    public CreateTranscriptSegmentDtoValidator()
    {
        RuleFor(x => x.VideoId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Video ID is required")
            .Must(BeValidGuid).WithMessage("Video ID must be a valid GUID format");

        RuleFor(x => x.Text)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Text is required")
            .MinimumLength(1).WithMessage("Text cannot be empty")
            .MaximumLength(10000).WithMessage("Text cannot exceed 10000 characters")
            .Must(NotContainControlCharacters).WithMessage("Text contains invalid control characters");

        RuleFor(x => x.StartTime)
            .GreaterThanOrEqualTo(0).WithMessage("Start time must be non-negative")
            .LessThan(86400).WithMessage("Start time cannot exceed 24 hours (86400 seconds)");

        RuleFor(x => x.EndTime)
            .GreaterThanOrEqualTo(0).WithMessage("End time must be non-negative")
            .LessThan(86400).WithMessage("End time cannot exceed 24 hours (86400 seconds)")
            .GreaterThan(x => x.StartTime).WithMessage("End time must be after start time");

        RuleFor(x => x.SegmentIndex)
            .GreaterThanOrEqualTo(0).WithMessage("Segment index must be non-negative")
            .LessThan(100000).WithMessage("Segment index seems unreasonably large");

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

        // Validate time range is reasonable
        RuleFor(x => x)
            .Must(HaveReasonableTimeRange)
            .WithMessage("Segment duration is invalid (too short or too long)")
            .WithName("TimeRange");
    }

    /// <summary>
    /// Validates that the string is a valid GUID format
    /// </summary>
    private bool BeValidGuid(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        return Guid.TryParse(id, out _);
    }

    /// <summary>
    /// Checks that text doesn't contain control characters (except common ones like newline)
    /// </summary>
    private bool NotContainControlCharacters(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;

        foreach (char c in text)
        {
            // Allow common whitespace characters
            if (c == '\n' || c == '\r' || c == '\t') continue;

            // Reject other control characters
            if (char.IsControl(c)) return false;
        }

        return true;
    }

    /// <summary>
    /// Validates that the time range is reasonable
    /// </summary>
    private bool HaveReasonableTimeRange(CreateTranscriptSegmentDto dto)
    {
        var duration = dto.EndTime - dto.StartTime;

        // Segment should be at least 0.1 seconds
        if (duration < 0.1) return false;

        // Single segment shouldn't be longer than 5 minutes (300 seconds)
        if (duration > 300) return false;

        return true;
    }
}