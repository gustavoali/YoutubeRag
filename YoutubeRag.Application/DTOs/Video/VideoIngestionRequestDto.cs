namespace YoutubeRag.Application.DTOs.Video;

public record VideoIngestionRequestDto(
    string Url,
    string UserId,
    string? Title = null,
    string? Description = null,
    ProcessingPriority Priority = ProcessingPriority.Normal
);
