using AutoMapper;
using YoutubeRag.Application.DTOs.User;
using YoutubeRag.Domain.Entities;

namespace YoutubeRag.Application.Mappings;

/// <summary>
/// AutoMapper profile for User entity mappings
/// </summary>
public class UserMappingProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the UserMappingProfile class
    /// </summary>
    public UserMappingProfile()
    {
        // Entity to DTO mappings
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.HasGoogleAuth,
                opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.GoogleId)))
            .ForMember(dest => dest.VideoCount,
                opt => opt.MapFrom(src => src.Videos != null ? src.Videos.Count : 0))
            .ForMember(dest => dest.JobCount,
                opt => opt.MapFrom(src => src.Jobs != null ? src.Jobs.Count : 0));

        CreateMap<User, UserProfileDto>()
            .ForMember(dest => dest.IsVerified,
                opt => opt.MapFrom(src => src.IsEmailVerified))
            .ForMember(dest => dest.PublicVideoCount,
                opt => opt.MapFrom(src => src.Videos != null ?
                    src.Videos.Count(v => v.Status == Domain.Enums.VideoStatus.Completed) : 0));

        // DTO to Entity mappings
        CreateMap<CreateUserDto, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // Will be set in service
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.IsEmailVerified, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.EmailVerificationToken, opt => opt.Ignore())
            .ForMember(dest => dest.EmailVerifiedAt, opt => opt.Ignore())
            .ForMember(dest => dest.GoogleId, opt => opt.Ignore())
            .ForMember(dest => dest.GoogleRefreshToken, opt => opt.Ignore())
            .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore())
            .ForMember(dest => dest.Videos, opt => opt.Ignore())
            .ForMember(dest => dest.Jobs, opt => opt.Ignore())
            .ForMember(dest => dest.RefreshTokens, opt => opt.Ignore());

        CreateMap<UpdateUserDto, User>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
    }
}
