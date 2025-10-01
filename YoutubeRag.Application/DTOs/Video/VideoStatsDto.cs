namespace YoutubeRag.Application.DTOs.Video;

/// <summary>
/// DTO for video statistics
/// </summary>
public record VideoStatsDto(
    string Id,
    int TotalTranscriptSegments,
    int TotalJobs,
    int CompletedJobs,
    int FailedJobs,
    double AverageConfidence,
    TimeSpan? Duration
);
