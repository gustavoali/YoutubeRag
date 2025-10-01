using AutoMapper;
using YoutubeRag.Application.DTOs.TranscriptSegment;
using YoutubeRag.Domain.Entities;

namespace YoutubeRag.Application.Mappings;

/// <summary>
/// AutoMapper profile for TranscriptSegment entity mappings
/// </summary>
public class TranscriptSegmentMappingProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the TranscriptSegmentMappingProfile class
    /// </summary>
    public TranscriptSegmentMappingProfile()
    {
        // Entity to DTO mappings
        CreateMap<TranscriptSegment, TranscriptSegmentDto>()
            .ForMember(dest => dest.HasEmbedding,
                opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.EmbeddingVector)));

        CreateMap<TranscriptSegment, TranscriptSegmentListDto>()
            .ForMember(dest => dest.TextSnippet,
                opt => opt.MapFrom(src => TruncateText(src.Text, 150)))
            .ForMember(dest => dest.HasEmbedding,
                opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.EmbeddingVector)));

        CreateMap<TranscriptSegment, TranscriptSearchResultDto>()
            .ForMember(dest => dest.VideoTitle,
                opt => opt.MapFrom(src => src.Video != null ? src.Video.Title : string.Empty))
            .ForMember(dest => dest.VideoThumbnailUrl,
                opt => opt.MapFrom(src => src.Video != null ? src.Video.ThumbnailUrl : null))
            .ForMember(dest => dest.HighlightedText,
                opt => opt.MapFrom(src => src.Text)) // Highlighting will be done in service
            .ForMember(dest => dest.YouTubeTimestampUrl,
                opt => opt.MapFrom(src => GenerateYouTubeTimestampUrl(src)))
            .ForMember(dest => dest.ContextSegments,
                opt => opt.Ignore()) // Will be populated in service
            .ForMember(dest => dest.RelevanceScore,
                opt => opt.Ignore()); // Will be calculated during search

        // DTO to Entity mappings
        CreateMap<CreateTranscriptSegmentDto, TranscriptSegment>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Video, opt => opt.Ignore())
            .ForMember(dest => dest.EmbeddingVector, opt => opt.Ignore()); // Will be generated separately

        CreateMap<UpdateTranscriptSegmentDto, TranscriptSegment>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
    }

    private static string? TruncateText(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        if (text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength - 3) + "...";
    }

    private static string? GenerateYouTubeTimestampUrl(TranscriptSegment segment)
    {
        if (segment.Video == null || string.IsNullOrEmpty(segment.Video.YoutubeId))
            return null;

        var timestamp = (int)segment.StartTime;
        return $"https://www.youtube.com/watch?v={segment.Video.YoutubeId}&t={timestamp}s";
    }
}