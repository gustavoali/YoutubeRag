using AutoMapper;
using YoutubeRag.Application.DTOs.Auth;
using YoutubeRag.Domain.Entities;

namespace YoutubeRag.Application.Mappings;

/// <summary>
/// AutoMapper profile for RefreshToken entity mappings
/// </summary>
public class RefreshTokenMappingProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the RefreshTokenMappingProfile class
    /// </summary>
    public RefreshTokenMappingProfile()
    {
        // Entity to DTO mappings
        CreateMap<RefreshToken, TokenResponseDto>()
            .ForMember(dest => dest.AccessToken, opt => opt.Ignore()) // Will be generated separately
            .ForMember(dest => dest.RefreshToken, opt => opt.MapFrom(src => src.Token))
            .ForMember(dest => dest.TokenType, opt => opt.MapFrom(src => "Bearer"))
            .ForMember(dest => dest.ExpiresIn, opt => opt.Ignore()) // Will be calculated
            .ForMember(dest => dest.RefreshTokenExpiresAt, opt => opt.MapFrom(src => src.ExpiresAt));
    }
}
