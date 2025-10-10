namespace YoutubeRag.Application.DTOs.Search;

/// <summary>
/// DTO for search request parameters
/// </summary>
public record SearchRequestDto(
    string Query,
    int? Limit = 10,
    int? Offset = 0,
    double? MinScore = 0.0
);
