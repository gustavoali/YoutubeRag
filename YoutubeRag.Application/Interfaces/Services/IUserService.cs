using YoutubeRag.Application.DTOs.Common;
using YoutubeRag.Application.DTOs.User;

namespace YoutubeRag.Application.Interfaces.Services;

/// <summary>
/// Service interface for user management operations
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Get user by ID
    /// </summary>
    Task<UserDto?> GetByIdAsync(string id);

    /// <summary>
    /// Get user by email
    /// </summary>
    Task<UserDto?> GetByEmailAsync(string email);

    /// <summary>
    /// Get paginated list of users
    /// </summary>
    Task<PaginatedResultDto<UserListDto>> GetAllAsync(int page = 1, int pageSize = 10);

    /// <summary>
    /// Create a new user
    /// </summary>
    Task<UserDto> CreateAsync(CreateUserDto createDto);

    /// <summary>
    /// Update an existing user
    /// </summary>
    Task<UserDto> UpdateAsync(string id, UpdateUserDto updateDto);

    /// <summary>
    /// Delete a user
    /// </summary>
    Task DeleteAsync(string id);

    /// <summary>
    /// Get user statistics
    /// </summary>
    Task<UserStatsDto> GetStatsAsync(string id);

    /// <summary>
    /// Check if user exists by email
    /// </summary>
    Task<bool> ExistsAsync(string email);
}
