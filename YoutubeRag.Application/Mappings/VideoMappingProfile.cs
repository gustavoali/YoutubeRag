using AutoMapper;
using YoutubeRag.Application.DTOs.Video;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Application.Mappings;

/// <summary>
/// AutoMapper profile for Video entity mappings
/// </summary>
public class VideoMappingProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the VideoMappingProfile class
    /// </summary>
    public VideoMappingProfile()
    {
        // Entity to DTO mappings
        CreateMap<Video, VideoDto>()
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.UserId,
                opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.TranscriptSegmentCount,
                opt => opt.MapFrom(src => src.TranscriptSegments != null ? src.TranscriptSegments.Count : 0))
            .ForMember(dest => dest.JobCount,
                opt => opt.MapFrom(src => src.Jobs != null ? src.Jobs.Count : 0));

        CreateMap<Video, VideoListDto>()
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.DescriptionSnippet,
                opt => opt.MapFrom(src => TruncateText(src.Description, 200)))
            .ForMember(dest => dest.OwnerName,
                opt => opt.MapFrom(src => src.User != null ? src.User.Name : string.Empty))
            .ForMember(dest => dest.OwnerId,
                opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.HasTranscripts,
                opt => opt.MapFrom(src => src.TranscriptSegments != null && src.TranscriptSegments.Any()))
            .ForMember(dest => dest.IsYouTubeVideo,
                opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.YouTubeId)));

        CreateMap<Video, VideoDetailsDto>()
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.CanEdit,
                opt => opt.Ignore()) // Will be set based on user permissions
            .ForMember(dest => dest.CanDelete,
                opt => opt.Ignore()) // Will be set based on user permissions
            .ForMember(dest => dest.CanProcess,
                opt => opt.MapFrom(src => src.Status == VideoStatus.Pending || src.Status == VideoStatus.Failed));

        // DTO to Entity mappings
        CreateMap<CreateVideoDto, Video>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => VideoStatus.Pending))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore()) // Will be set in service
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.Jobs, opt => opt.Ignore())
            .ForMember(dest => dest.TranscriptSegments, opt => opt.Ignore())
            .ForMember(dest => dest.FilePath, opt => opt.Ignore())
            .ForMember(dest => dest.AudioPath, opt => opt.Ignore())
            .ForMember(dest => dest.ProcessingLog, opt => opt.Ignore())
            .ForMember(dest => dest.ErrorMessage, opt => opt.Ignore())
            .ForMember(dest => dest.ProcessingProgress, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.YouTubeId, opt => opt.Ignore()) // Will be extracted from URL
            .ForMember(dest => dest.OriginalUrl, opt => opt.MapFrom(src => src.YoutubeUrl))
            .ForMember(dest => dest.Duration, opt => opt.Ignore())
            .ForMember(dest => dest.ViewCount, opt => opt.Ignore())
            .ForMember(dest => dest.LikeCount, opt => opt.Ignore());

        CreateMap<UpdateVideoDto, Video>()
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
