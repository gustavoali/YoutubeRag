namespace YoutubeRag.Application.DTOs.User;

/// <summary>
/// DTO for user list item (lightweight version)
/// </summary>
public record UserListDto(
    string Id,
    string Name,
    string Email,
    bool IsActive,
    DateTime CreatedAt
);
