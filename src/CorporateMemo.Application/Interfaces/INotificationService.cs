using CorporateMemo.Domain.Entities;
using CorporateMemo.Domain.Enums;

namespace CorporateMemo.Application.Interfaces;

/// <summary>
/// Defines the contract for managing in-app notifications.
/// Notifications are stored in the database and displayed in the notification bell/panel.
/// This interface is implemented in the Infrastructure layer using EF Core.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Creates and persists a new notification for a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user who will receive the notification.</param>
    /// <param name="message">The human-readable notification message text.</param>
    /// <param name="type">The type of notification (determines icon and behavior when clicked).</param>
    /// <param name="memoId">Optional: the ID of the related memo (for navigation when clicked).</param>
    /// <param name="ct">A cancellation token to cancel the async operation if needed.</param>
    /// <returns>The created notification entity.</returns>
    Task<Notification> CreateNotificationAsync(
        string userId,
        string message,
        NotificationType type,
        Guid? memoId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the count of unread notifications for a specific user.
    /// This number is displayed as a badge on the notification bell icon.
    /// </summary>
    /// <param name="userId">The ID of the user to count unread notifications for.</param>
    /// <param name="ct">A cancellation token to cancel the async operation if needed.</param>
    /// <returns>The number of unread notifications for the user.</returns>
    Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves the most recent notifications for a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user to retrieve notifications for.</param>
    /// <param name="count">Maximum number of notifications to return (default: 20).</param>
    /// <param name="ct">A cancellation token to cancel the async operation if needed.</param>
    /// <returns>A list of notifications ordered by most recent first.</returns>
    Task<List<Notification>> GetNotificationsAsync(string userId, int count = 20, CancellationToken ct = default);

    /// <summary>
    /// Marks all notifications as read for a specific user.
    /// Called when the user opens the notification panel.
    /// </summary>
    /// <param name="userId">The ID of the user whose notifications should be marked as read.</param>
    /// <param name="ct">A cancellation token to cancel the async operation if needed.</param>
    Task MarkAllReadAsync(string userId, CancellationToken ct = default);
}
