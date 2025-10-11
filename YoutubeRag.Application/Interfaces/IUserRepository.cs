using YoutubeRag.Domain.Entities;

namespace YoutubeRag.Application.Interfaces;

/// <summary>
/// Repository interface for User entity operations
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Gets a user by their email address
    /// </summary>
    /// <param name="email">The email address to search for</param>
    /// <returns>The user if found; otherwise, null</returns>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Gets a user by their Google OAuth identifier
    /// </summary>
    /// <param name="googleId">The Google OAuth identifier</param>
    /// <returns>The user if found; otherwise, null</returns>
    Task<User?> GetByGoogleIdAsync(string googleId);

    /// <summary>
    /// Gets a user with their associated videos
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <returns>The user with loaded videos if found; otherwise, null</returns>
    Task<User?> GetWithVideosAsync(string userId);

    /// <summary>
    /// Gets a user with their associated jobs
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <returns>The user with loaded jobs if found; otherwise, null</returns>
    Task<User?> GetWithJobsAsync(string userId);

    /// <summary>
    /// Gets all active users
    /// </summary>
    /// <returns>A collection of active users</returns>
    Task<IEnumerable<User>> GetActiveUsersAsync();

    /// <summary>
    /// Checks if an email is already in use
    /// </summary>
    /// <param name="email">The email address to check</param>
    /// <returns>True if the email is in use; otherwise, false</returns>
    Task<bool> EmailExistsAsync(string email);

    /// <summary>
    /// Gets a user by their email verification token
    /// </summary>
    /// <param name="token">The email verification token</param>
    /// <returns>The user if found; otherwise, null</returns>
    Task<User?> GetByEmailVerificationTokenAsync(string token);

    /// <summary>
    /// Gets users who have logged in within a specific time period
    /// </summary>
    /// <param name="since">The start date to check last login</param>
    /// <returns>A collection of users who logged in since the specified date</returns>
    Task<IEnumerable<User>> GetRecentlyActiveUsersAsync(DateTime since);
}
