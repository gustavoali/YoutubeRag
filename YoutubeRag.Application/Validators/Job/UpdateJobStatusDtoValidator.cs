using System.Text.Json;
using FluentValidation;
using YoutubeRag.Application.DTOs.Job;

namespace YoutubeRag.Application.Validators.Job;

/// <summary>
/// Validator for update job status DTO
/// </summary>
public class UpdateJobStatusDtoValidator : AbstractValidator<UpdateJobStatusDto>
{
    private readonly string[] _validStatuses = new[]
    {
        "pending",
        "running",
        "completed",
        "failed",
        "cancelled",
        "paused",
        "retrying"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateJobStatusDtoValidator"/> class
    /// </summary>
    public UpdateJobStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Status is required")
            .Must(BeValidStatus).WithMessage($"Invalid status. Valid statuses are: {string.Join(", ", _validStatuses)}");

        When(x => !string.IsNullOrEmpty(x.StatusMessage), () =>
        {
            RuleFor(x => x.StatusMessage)
                .MaximumLength(500).WithMessage("Status message cannot exceed 500 characters");
        });

        When(x => x.Progress.HasValue, () =>
        {
            RuleFor(x => x.Progress)
                .InclusiveBetween(0, 100).WithMessage("Progress must be between 0 and 100");
        });

        // Error message is required when status is "failed"
        When(x => x.Status == "failed", () =>
        {
            RuleFor(x => x.ErrorMessage)
                .NotEmpty().WithMessage("Error message is required when status is 'failed'")
                .MaximumLength(2000).WithMessage("Error message cannot exceed 2000 characters");
        });

        // Error message should not be provided for successful statuses
        When(x => x.Status == "completed" || x.Status == "running", () =>
        {
            RuleFor(x => x.ErrorMessage)
                .Empty().WithMessage($"Error message should not be provided when status is '{nameof(UpdateJobStatusDto.Status)}'");
        });

        When(x => !string.IsNullOrEmpty(x.Result), () =>
        {
            RuleFor(x => x.Result)
                .Must(BeValidJson).WithMessage("Result must be valid JSON format")
                .MaximumLength(50000).WithMessage("Result cannot exceed 50000 characters");
        });

        // Result should only be provided when job is completed
        When(x => !string.IsNullOrEmpty(x.Result) && x.Status != "completed", () =>
        {
            RuleFor(x => x.Result)
                .Empty().WithMessage("Result should only be provided when status is 'completed'");
        });

        When(x => !string.IsNullOrEmpty(x.Metadata), () =>
        {
            RuleFor(x => x.Metadata)
                .Must(BeValidJson).WithMessage("Metadata must be valid JSON format")
                .MaximumLength(10000).WithMessage("Metadata cannot exceed 10000 characters");
        });

        // Progress validation based on status
        RuleFor(x => x)
            .Must(HaveValidProgressForStatus)
            .WithMessage("Progress value is inconsistent with the job status")
            .WithName("Progress");
    }

    /// <summary>
    /// Validates that the status is one of the allowed values
    /// </summary>
    private bool BeValidStatus(string status)
    {
        if (string.IsNullOrEmpty(status))
        {
            return false;
        }

        return _validStatuses.Contains(status.ToLowerInvariant());
    }

    /// <summary>
    /// Validates that the string is valid JSON
    /// </summary>
    private bool BeValidJson(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return true;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Validates that progress is consistent with status
    /// </summary>
    private bool HaveValidProgressForStatus(UpdateJobStatusDto dto)
    {
        if (!dto.Progress.HasValue)
        {
            return true;
        }

        switch (dto.Status?.ToLowerInvariant())
        {
            case "pending":
                return dto.Progress == 0;
            case "completed":
                return dto.Progress == 100;
            case "failed":
            case "cancelled":
                // Failed or cancelled jobs can have any progress
                return true;
            case "running":
            case "paused":
            case "retrying":
                // Running/paused/retrying jobs should have progress between 0 and 100
                return dto.Progress >= 0 && dto.Progress <= 100;
            default:
                return true;
        }
    }
}
