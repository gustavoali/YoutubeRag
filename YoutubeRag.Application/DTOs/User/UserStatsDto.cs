namespace YoutubeRag.Application.DTOs.User;

/// <summary>
/// DTO for user statistics
/// </summary>
public record UserStatsDto(
    string Id,
    int TotalVideos,
    int TotalJobs,
    int CompletedJobs,
    int FailedJobs,
    long TotalStorageBytes,
    DateTime MemberSince
);
