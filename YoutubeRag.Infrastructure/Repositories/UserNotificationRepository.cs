using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;
using YoutubeRag.Infrastructure.Data;

namespace YoutubeRag.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for UserNotification persistence operations
/// </summary>
public class UserNotificationRepository : IUserNotificationRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserNotificationRepository> _logger;

    public UserNotificationRepository(
        ApplicationDbContext context,
        ILogger<UserNotificationRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<UserNotification> AddAsync(UserNotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (string.IsNullOrWhiteSpace(notification.Id))
        {
            notification.Id = Guid.NewGuid().ToString();
        }

        notification.CreatedAt = DateTime.UtcNow;
        notification.UpdatedAt = DateTime.UtcNow;
        notification.IsRead = false;

        _context.UserNotifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Added notification {NotificationId} for user {UserId} (Type: {Type})",
            notification.Id, notification.UserId ?? "ALL", notification.Type);

        return notification;
    }

    /// <inheritdoc />
    public async Task<List<UserNotification>> GetByUserIdAsync(
        string userId,
        int limit = 50,
        NotificationType? type = null,
        bool? isRead = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        if (limit < 1 || limit > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be between 1 and 100");
        }

        var query = _context.UserNotifications
            .Where(n => n.UserId == userId || n.UserId == null);  // Include broadcasts

        // Apply optional filters
        if (type.HasValue)
        {
            query = query.Where(n => n.Type == type.Value);
        }

        if (isRead.HasValue)
        {
            query = query.Where(n => n.IsRead == isRead.Value);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Retrieved {Count} notifications for user {UserId} (Type: {Type}, IsRead: {IsRead})",
            notifications.Count, userId, type?.ToString() ?? "ALL", isRead?.ToString() ?? "ALL");

        return notifications;
    }

    /// <inheritdoc />
    public async Task<List<UserNotification>> GetUnreadByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var notifications = await _context.UserNotifications
            .Where(n => (n.UserId == userId || n.UserId == null) && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Retrieved {Count} unread notifications for user {UserId}", notifications.Count, userId);

        return notifications;
    }

    /// <inheritdoc />
    public async Task<UserNotification?> GetByIdAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(notificationId);

        var notification = await _context.UserNotifications
            .Include(n => n.Job)
            .Include(n => n.Video)
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (notification != null)
        {
            _logger.LogDebug("Retrieved notification {NotificationId}", notificationId);
        }
        else
        {
            _logger.LogDebug("Notification {NotificationId} not found", notificationId);
        }

        return notification;
    }

    /// <inheritdoc />
    public async Task<bool> MarkAsReadAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(notificationId);

        var notification = await _context.UserNotifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (notification == null)
        {
            _logger.LogDebug("Notification {NotificationId} not found for marking as read", notificationId);
            return false;
        }

        if (notification.IsRead)
        {
            _logger.LogDebug("Notification {NotificationId} already marked as read", notificationId);
            return true;
        }

        notification.MarkAsRead();

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Marked notification {NotificationId} as read", notificationId);

        return true;
    }

    /// <inheritdoc />
    public async Task<int> MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var notifications = await _context.UserNotifications
            .Where(n => (n.UserId == userId || n.UserId == null) && !n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in notifications)
        {
            notification.MarkAsRead();
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Marked {Count} notifications as read for user {UserId}", notifications.Count, userId);

        return notifications.Count;
    }

    /// <inheritdoc />
    public async Task<int> DeleteOldNotificationsAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.Subtract(olderThan);

        var notifications = await _context.UserNotifications
            .Where(n => n.CreatedAt < cutoffDate && n.IsRead)
            .ToListAsync(cancellationToken);

        _context.UserNotifications.RemoveRange(notifications);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted {Count} old notifications older than {CutoffDate}", notifications.Count, cutoffDate);

        return notifications.Count;
    }

    /// <inheritdoc />
    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var count = await _context.UserNotifications
            .CountAsync(n => (n.UserId == userId || n.UserId == null) && !n.IsRead, cancellationToken);

        _logger.LogDebug("Unread notification count for user {UserId}: {Count}", userId, count);

        return count;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(notificationId);

        var notification = await _context.UserNotifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (notification == null)
        {
            _logger.LogDebug("Notification {NotificationId} not found for deletion", notificationId);
            return false;
        }

        _context.UserNotifications.Remove(notification);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Deleted notification {NotificationId}", notificationId);

        return true;
    }
}
