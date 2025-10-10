using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YoutubeRag.Application.Interfaces;

namespace YoutubeRag.Infrastructure.Jobs;

/// <summary>
/// Background job that cleans up old read notifications from the database
/// to prevent unbounded growth of the UserNotifications table
/// </summary>
public class NotificationCleanupJob
{
    private readonly IUserNotificationRepository _notificationRepository;
    private readonly ILogger<NotificationCleanupJob> _logger;
    private readonly CleanupOptions _options;

    public NotificationCleanupJob(
        IUserNotificationRepository notificationRepository,
        ILogger<NotificationCleanupJob> logger,
        IOptions<CleanupOptions> options)
    {
        _notificationRepository = notificationRepository;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Executes the notification cleanup process
    /// Deletes read notifications older than the configured retention period
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting notification cleanup process");

        try
        {
            // Calculate retention period
            var retentionDays = _options.NotificationRetentionDays;
            var olderThan = TimeSpan.FromDays(retentionDays);

            _logger.LogInformation("Cleaning up read notifications older than {Days} days", retentionDays);

            // Use existing repository method to delete old notifications
            var deletedCount = await _notificationRepository.DeleteOldNotificationsAsync(
                olderThan,
                cancellationToken);

            _logger.LogInformation(
                "Notification cleanup completed. Deleted {Count} old read notifications",
                deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Notification cleanup process failed: {Error}", ex.Message);
            throw;
        }
    }
}
