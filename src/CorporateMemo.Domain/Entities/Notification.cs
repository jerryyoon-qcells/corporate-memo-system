using CorporateMemo.Domain.Enums;

namespace CorporateMemo.Domain.Entities;

/// <summary>
/// Represents an in-app notification delivered to a user.
/// Notifications appear in the notification bell/panel in the navigation bar.
/// They are also stored in the database for persistence across sessions.
/// </summary>
public class Notification
{
    /// <summary>
    /// Gets or sets the unique identifier for this notification.
    /// A new Guid is assigned automatically when the notification is created.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the unique user ID of the notification recipient.
    /// This maps to the ApplicationUser.Id field in ASP.NET Core Identity.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable notification message text.
    /// Example: "Memo JSMITH-20260302-001 has been approved by Jane Smith."
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of notification, which determines the icon and action.
    /// See the NotificationType enum for possible values.
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// Gets or sets whether the user has read this notification.
    /// Unread notifications contribute to the badge count on the notification bell.
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this notification was created.
    /// Used to display relative timestamps like "2 hours ago" in the notification panel.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the optional identifier of the memo related to this notification.
    /// When the user clicks the notification, the application navigates to this memo.
    /// Null if the notification is not related to a specific memo.
    /// </summary>
    public Guid? MemoId { get; set; }
}
