using YoutubeRag.Domain.Entities;

namespace YoutubeRag.Application.Interfaces;

/// <summary>
/// Repository interface for RefreshToken entity operations
/// </summary>
public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    /// <summary>
    /// Gets a refresh token by its token value
    /// </summary>
    /// <param name="token">The refresh token string</param>
    /// <returns>The refresh token if found; otherwise, null</returns>
    Task<RefreshToken?> GetByTokenAsync(string token);

    /// <summary>
    /// Gets all active refresh tokens for a user
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <returns>A collection of active refresh tokens for the user</returns>
    Task<IEnumerable<RefreshToken>> GetActiveByUserIdAsync(string userId);

    /// <summary>
    /// Gets all refresh tokens for a user (including revoked ones)
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <returns>A collection of all refresh tokens for the user</returns>
    Task<IEnumerable<RefreshToken>> GetByUserIdAsync(string userId);

    /// <summary>
    /// Gets a refresh token with its associated user
    /// </summary>
    /// <param name="token">The refresh token string</param>
    /// <returns>The refresh token with loaded user if found; otherwise, null</returns>
    Task<RefreshToken?> GetByTokenWithUserAsync(string token);

    /// <summary>
    /// Revokes all active refresh tokens for a user
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <param name="reason">The reason for revocation</param>
    /// <returns>The number of tokens revoked</returns>
    Task<int> RevokeAllByUserIdAsync(string userId, string reason = "User requested");

    /// <summary>
    /// Revokes a specific refresh token
    /// </summary>
    /// <param name="token">The refresh token to revoke</param>
    /// <param name="reason">The reason for revocation</param>
    /// <returns>True if the token was revoked; false if not found</returns>
    Task<bool> RevokeByTokenAsync(string token, string reason = "Token revoked");

    /// <summary>
    /// Gets expired but not revoked tokens for cleanup
    /// </summary>
    /// <returns>A collection of expired tokens</returns>
    Task<IEnumerable<RefreshToken>> GetExpiredTokensAsync();

    /// <summary>
    /// Deletes expired tokens older than a specified date
    /// </summary>
    /// <param name="olderThan">Delete tokens expired before this date</param>
    /// <returns>The number of tokens deleted</returns>
    Task<int> DeleteExpiredTokensAsync(DateTime olderThan);

    /// <summary>
    /// Gets refresh tokens by device info
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <param name="deviceInfo">The device information</param>
    /// <returns>A collection of refresh tokens from the specified device</returns>
    Task<IEnumerable<RefreshToken>> GetByUserIdAndDeviceAsync(string userId, string deviceInfo);

    /// <summary>
    /// Gets refresh tokens by IP address
    /// </summary>
    /// <param name="ipAddress">The IP address</param>
    /// <returns>A collection of refresh tokens from the specified IP address</returns>
    Task<IEnumerable<RefreshToken>> GetByIpAddressAsync(string ipAddress);

    /// <summary>
    /// Checks if a refresh token is valid and active
    /// </summary>
    /// <param name="token">The refresh token to validate</param>
    /// <returns>True if the token is valid and active; otherwise, false</returns>
    Task<bool> IsTokenValidAsync(string token);

    /// <summary>
    /// Gets the count of active tokens for a user
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <returns>The count of active tokens</returns>
    Task<int> GetActiveTokenCountByUserIdAsync(string userId);
}