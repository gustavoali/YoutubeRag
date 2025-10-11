using AutoMapper;
using YoutubeRag.Application.DTOs.Job;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Application.Mappings;

/// <summary>
/// AutoMapper profile for Job entity mappings
/// </summary>
public class JobMappingProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the JobMappingProfile class
    /// </summary>
    public JobMappingProfile()
    {
        // Entity to DTO mappings
        CreateMap<Job, JobDto>()
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<Job, JobStatusDto>()
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.IsComplete,
                opt => opt.MapFrom(src => src.Status == JobStatus.Completed ||
                                         src.Status == JobStatus.Failed ||
                                         src.Status == JobStatus.Cancelled))
            .ForMember(dest => dest.IsRunning,
                opt => opt.MapFrom(src => src.Status == JobStatus.Running ||
                                         src.Status == JobStatus.Retrying))
            .ForMember(dest => dest.EstimatedTimeRemaining,
                opt => opt.Ignore()); // Could be calculated based on progress and elapsed time

        CreateMap<Job, JobListDto>()
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.VideoTitle,
                opt => opt.MapFrom(src => src.Video != null ? src.Video.Title : null))
            .ForMember(dest => dest.HasError,
                opt => opt.MapFrom(src => src.Status == JobStatus.Failed && !string.IsNullOrEmpty(src.ErrorMessage)))
            .ForMember(dest => dest.ErrorMessage,
                opt => opt.MapFrom(src => TruncateText(src.ErrorMessage, 200)));

        // DTO to Entity mappings
        CreateMap<CreateJobDto, Job>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.StartImmediately ? JobStatus.Pending : JobStatus.Pending))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore()) // Will be set in service
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.Video, opt => opt.Ignore())
            .ForMember(dest => dest.StatusMessage, opt => opt.Ignore())
            .ForMember(dest => dest.Progress, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.Result, opt => opt.Ignore())
            .ForMember(dest => dest.ErrorMessage, opt => opt.Ignore())
            .ForMember(dest => dest.StartedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CompletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.RetryCount, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.WorkerId, opt => opt.Ignore());

        CreateMap<UpdateJobStatusDto, Job>()
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => Enum.Parse<JobStatus>(src.Status)))
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
    }

    private static string? TruncateText(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        if (text.Length <= maxLength)
        {
            return text;
        }

        return text.Substring(0, maxLength - 3) + "...";
    }
}
