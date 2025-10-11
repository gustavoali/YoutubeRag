using System.Text.Json;
using FluentValidation;
using YoutubeRag.Application.DTOs.Job;

namespace YoutubeRag.Application.Validators.Job;

/// <summary>
/// Validator for create job DTO
/// </summary>
public class CreateJobDtoValidator : AbstractValidator<CreateJobDto>
{
    private readonly string[] _validJobTypes = new[]
    {
        "video_processing",
        "transcription",
        "embedding",
        "thumbnail_generation",
        "metadata_extraction",
        "cleanup",
        "reindex"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateJobDtoValidator"/> class
    /// </summary>
    public CreateJobDtoValidator()
    {
        RuleFor(x => x.JobType)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Job type is required")
            .Length(1, 100).WithMessage("Job type must be between 1 and 100 characters")
            .Must(BeValidJobType).WithMessage($"Invalid job type. Valid types are: {string.Join(", ", _validJobTypes)}");

        When(x => !string.IsNullOrEmpty(x.VideoId), () =>
        {
            RuleFor(x => x.VideoId)
                .Must(BeValidGuid).WithMessage("Video ID must be a valid GUID format")
                .MaximumLength(50).WithMessage("Video ID cannot exceed 50 characters");
        });

        // Certain job types require a video ID
        When(x => x.JobType == "video_processing" || x.JobType == "transcription" || x.JobType == "embedding", () =>
        {
            RuleFor(x => x.VideoId)
                .NotEmpty().WithMessage($"Video ID is required for job type '{nameof(CreateJobDto.JobType)}'");
        });

        When(x => !string.IsNullOrEmpty(x.Parameters), () =>
        {
            RuleFor(x => x.Parameters)
                .Must(BeValidJson).WithMessage("Parameters must be valid JSON format")
                .MaximumLength(10000).WithMessage("Parameters cannot exceed 10000 characters");
        });

        When(x => !string.IsNullOrEmpty(x.Metadata), () =>
        {
            RuleFor(x => x.Metadata)
                .Must(BeValidJson).WithMessage("Metadata must be valid JSON format")
                .MaximumLength(10000).WithMessage("Metadata cannot exceed 10000 characters");
        });

        RuleFor(x => x.MaxRetries)
            .InclusiveBetween(0, 10).WithMessage("Max retries must be between 0 and 10");

        RuleFor(x => x.Priority)
            .InclusiveBetween(0, 100).WithMessage("Priority must be between 0 and 100");

        // Validate job-specific parameters based on job type
        RuleFor(x => x)
            .Must(HaveValidJobSpecificParameters)
            .WithMessage("Invalid parameters for the specified job type")
            .WithName("JobParameters");
    }

    /// <summary>
    /// Validates that the job type is one of the allowed values
    /// </summary>
    private bool BeValidJobType(string jobType)
    {
        if (string.IsNullOrEmpty(jobType))
        {
            return false;
        }

        return _validJobTypes.Contains(jobType.ToLowerInvariant());
    }

    /// <summary>
    /// Validates that the string is a valid GUID format
    /// </summary>
    private bool BeValidGuid(string? id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }

        return Guid.TryParse(id, out _);
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
    /// Validates job-specific parameters based on job type
    /// </summary>
    private bool HaveValidJobSpecificParameters(CreateJobDto dto)
    {
        if (string.IsNullOrEmpty(dto.Parameters))
        {
            return true;
        }

        try
        {
            using var doc = JsonDocument.Parse(dto.Parameters);
            var root = doc.RootElement;

            switch (dto.JobType?.ToLowerInvariant())
            {
                case "transcription":
                    // Validate transcription parameters
                    if (root.TryGetProperty("language", out var lang))
                    {
                        var langValue = lang.GetString();
                        if (!string.IsNullOrEmpty(langValue) && !System.Text.RegularExpressions.Regex.IsMatch(langValue, @"^[a-z]{2}(-[A-Z]{2})?$"))
                        {
                            return false;
                        }
                    }

                    break;

                case "embedding":
                    // Validate embedding parameters
                    if (root.TryGetProperty("model", out var model))
                    {
                        var modelValue = model.GetString();
                        var validModels = new[] { "text-embedding-ada-002", "text-embedding-3-small", "text-embedding-3-large" };
                        if (!string.IsNullOrEmpty(modelValue) && !validModels.Contains(modelValue))
                        {
                            return false;
                        }
                    }

                    break;

                case "video_processing":
                    // Validate video processing parameters
                    if (root.TryGetProperty("quality", out var quality))
                    {
                        var qualityValue = quality.GetString();
                        var validQualities = new[] { "low", "medium", "high", "auto" };
                        if (!string.IsNullOrEmpty(qualityValue) && !validQualities.Contains(qualityValue.ToLowerInvariant()))
                        {
                            return false;
                        }
                    }

                    break;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
