using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Infrastructure.Data;

namespace YoutubeRag.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for RefreshToken entity operations
/// </summary>
public class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
{
    /// <summary>
    /// Initializes a new instance of the RefreshTokenRepository class
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="logger">The logger instance</param>
    public RefreshTokenRepository(ApplicationDbContext context, ILogger<RefreshTokenRepository> logger)
        : base(context, logger)
    {
    }

    /// <inheritdoc />
    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token cannot be null or empty", nameof(token));
        }

        try
        {
            return await _dbSet
                .FirstOrDefaultAsync(rt => rt.Token == token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving refresh token by token value");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RefreshToken>> GetActiveByUserIdAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        try
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Where(rt => rt.UserId == userId &&
                            !rt.IsRevoked &&
                            rt.ExpiresAt > now)
                .OrderByDescending(rt => rt.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active refresh tokens for user ID {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RefreshToken>> GetByUserIdAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        try
        {
            return await _dbSet
                .Where(rt => rt.UserId == userId)
                .OrderByDescending(rt => rt.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving refresh tokens for user ID {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<RefreshToken?> GetByTokenWithUserAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token cannot be null or empty", nameof(token));
        }

        try
        {
            return await _dbSet
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving refresh token with user by token value");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> RevokeAllByUserIdAsync(string userId, string reason = "User requested")
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        try
        {
            var now = DateTime.UtcNow;
            var activeTokens = await _dbSet
                .Where(rt => rt.UserId == userId &&
                            !rt.IsRevoked &&
                            rt.ExpiresAt > now)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = now;
                token.RevokedReason = reason;
            }

            return activeTokens.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all refresh tokens for user ID {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RevokeByTokenAsync(string token, string reason = "Token revoked")
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token cannot be null or empty", nameof(token));
        }

        try
        {
            var refreshToken = await GetByTokenAsync(token);
            if (refreshToken == null)
            {
                return false;
            }

            if (!refreshToken.IsRevoked)
            {
                refreshToken.IsRevoked = true;
                refreshToken.RevokedAt = DateTime.UtcNow;
                refreshToken.RevokedReason = reason;
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking refresh token");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RefreshToken>> GetExpiredTokensAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Where(rt => rt.ExpiresAt <= now && !rt.IsRevoked)
                .OrderBy(rt => rt.ExpiresAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expired refresh tokens");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> DeleteExpiredTokensAsync(DateTime olderThan)
    {
        try
        {
            var tokensToDelete = await _dbSet
                .Where(rt => rt.ExpiresAt < olderThan)
                .ToListAsync();

            if (tokensToDelete.Any())
            {
                _dbSet.RemoveRange(tokensToDelete);
            }

            return tokensToDelete.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expired refresh tokens older than {OlderThan}", olderThan);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RefreshToken>> GetByUserIdAndDeviceAsync(string userId, string deviceInfo)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(deviceInfo))
        {
            throw new ArgumentException("Device info cannot be null or empty", nameof(deviceInfo));
        }

        try
        {
            return await _dbSet
                .Where(rt => rt.UserId == userId && rt.DeviceInfo == deviceInfo)
                .OrderByDescending(rt => rt.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving refresh tokens for user ID {UserId} and device {DeviceInfo}",
                userId, deviceInfo);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RefreshToken>> GetByIpAddressAsync(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            throw new ArgumentException("IP address cannot be null or empty", nameof(ipAddress));
        }

        try
        {
            return await _dbSet
                .Where(rt => rt.IpAddress == ipAddress)
                .OrderByDescending(rt => rt.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving refresh tokens for IP address {IpAddress}", ipAddress);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsTokenValidAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token cannot be null or empty", nameof(token));
        }

        try
        {
            var refreshToken = await GetByTokenAsync(token);
            return refreshToken != null && refreshToken.IsActive;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating refresh token");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> GetActiveTokenCountByUserIdAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        try
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .CountAsync(rt => rt.UserId == userId &&
                                 !rt.IsRevoked &&
                                 rt.ExpiresAt > now);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting active refresh tokens for user ID {UserId}", userId);
            throw;
        }
    }
}
