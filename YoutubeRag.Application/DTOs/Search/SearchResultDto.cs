namespace YoutubeRag.Application.DTOs.Search;

/// <summary>
/// DTO for individual search result
/// </summary>
public record SearchResultDto(
    string VideoId,
    string VideoTitle,
    string SegmentId,
    string SegmentText,
    double StartTime,
    double EndTime,
    double Score,
    double Timestamp
);
