using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Application.Interfaces;

/// <summary>
/// Repository interface for UserNotification persistence operations
/// </summary>
public interface IUserNotificationRepository
{
    /// <summary>
    /// Adds a new notification to the database
    /// </summary>
    /// <param name="notification">The notification to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The added notification with generated ID</returns>
    Task<UserNotification> AddAsync(UserNotification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notifications for a specific user (includes broadcast notifications)
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="limit">Maximum number of notifications to return (default: 50)</param>
    /// <param name="type">Optional: Filter by notification type</param>
    /// <param name="isRead">Optional: Filter by read status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of notifications ordered by creation date (newest first)</returns>
    Task<List<UserNotification>> GetByUserIdAsync(
        string userId,
        int limit = 50,
        NotificationType? type = null,
        bool? isRead = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unread notifications for a specific user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of unread notifications</returns>
    Task<List<UserNotification>> GetUnreadByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a notification by its ID
    /// </summary>
    /// <param name="notificationId">The notification ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The notification or null if not found</returns>
    Task<UserNotification?> GetByIdAsync(string notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a notification as read
    /// </summary>
    /// <param name="notificationId">The notification ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful, false if notification not found</returns>
    Task<bool> MarkAsReadAsync(string notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks all unread notifications for a user as read
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of notifications marked as read</returns>
    Task<int> MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes old read notifications
    /// </summary>
    /// <param name="olderThan">Delete notifications older than this timespan</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of notifications deleted</returns>
    Task<int> DeleteOldNotificationsAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of unread notifications for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of unread notifications</returns>
    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a notification by ID
    /// </summary>
    /// <param name="notificationId">The notification ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful, false if notification not found</returns>
    Task<bool> DeleteAsync(string notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent notifications for a specific job within a time window
    /// Used for deduplication
    /// </summary>
    /// <param name="jobId">The job ID</param>
    /// <param name="timeWindow">Time window to look back</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of recent notifications for the job</returns>
    Task<List<UserNotification>> GetByJobIdRecentAsync(
        string jobId,
        TimeSpan timeWindow,
        CancellationToken cancellationToken = default);
}
