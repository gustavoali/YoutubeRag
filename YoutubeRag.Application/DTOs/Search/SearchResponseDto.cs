namespace YoutubeRag.Application.DTOs.Search;

/// <summary>
/// DTO for search response with results and metadata
/// </summary>
public record SearchResponseDto(
    string Query,
    List<SearchResultDto> Results,
    int TotalResults,
    int Limit,
    int Offset
);
