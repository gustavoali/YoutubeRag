using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Infrastructure.Data;

namespace YoutubeRag.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for User entity operations
/// </summary>
public class UserRepository : Repository<User>, IUserRepository
{
    /// <summary>
    /// Initializes a new instance of the UserRepository class
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="logger">The logger instance</param>
    public UserRepository(ApplicationDbContext context, ILogger<UserRepository> logger)
        : base(context, logger)
    {
    }

    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be null or empty", nameof(email));
        }

        try
        {
            return await _dbSet
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by email {Email}", email);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<User?> GetByGoogleIdAsync(string googleId)
    {
        if (string.IsNullOrWhiteSpace(googleId))
        {
            throw new ArgumentException("Google ID cannot be null or empty", nameof(googleId));
        }

        try
        {
            return await _dbSet
                .FirstOrDefaultAsync(u => u.GoogleId == googleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by Google ID {GoogleId}", googleId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<User?> GetWithVideosAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        try
        {
            return await _dbSet
                .Include(u => u.Videos)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user with videos for user ID {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<User?> GetWithJobsAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        try
        {
            return await _dbSet
                .Include(u => u.Jobs)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user with jobs for user ID {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        try
        {
            return await _dbSet
                .Where(u => u.IsActive)
                .OrderBy(u => u.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active users");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> EmailExistsAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be null or empty", nameof(email));
        }

        try
        {
            return await _dbSet
                .AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if email exists {Email}", email);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<User?> GetByEmailVerificationTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token cannot be null or empty", nameof(token));
        }

        try
        {
            return await _dbSet
                .FirstOrDefaultAsync(u => u.EmailVerificationToken == token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by email verification token");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<User>> GetRecentlyActiveUsersAsync(DateTime since)
    {
        try
        {
            return await _dbSet
                .Where(u => u.IsActive && u.LastLoginAt != null && u.LastLoginAt >= since)
                .OrderByDescending(u => u.LastLoginAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recently active users since {Since}", since);
            throw;
        }
    }
}
