using CorporateMemo.Domain.Enums;

namespace CorporateMemo.Application.DTOs;

/// <summary>
/// Data Transfer Object for in-app notifications displayed in the notification panel.
/// Maps from the Notification domain entity.
/// </summary>
public class NotificationDto
{
    /// <summary>Gets or sets the unique identifier of the notification.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the user ID of the recipient.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Gets or sets the human-readable notification message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the type of notification (determines icon and click behavior).</summary>
    public NotificationType Type { get; set; }

    /// <summary>Gets or sets whether the user has read this notification.</summary>
    public bool IsRead { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the notification was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the optional memo ID for navigation when the notification is clicked.</summary>
    public Guid? MemoId { get; set; }

    /// <summary>
    /// Gets a human-friendly relative time string (e.g., "2 hours ago", "Just now").
    /// Computed from the CreatedAt timestamp and the current UTC time.
    /// </summary>
    public string RelativeTime
    {
        get
        {
            // Calculate how long ago the notification was created
            var elapsed = DateTime.UtcNow - CreatedAt;

            return elapsed.TotalSeconds switch
            {
                // Less than 1 minute ago: "Just now"
                < 60 => "Just now",
                // Less than 1 hour ago: show minutes
                < 3600 => $"{(int)elapsed.TotalMinutes} minute{((int)elapsed.TotalMinutes == 1 ? "" : "s")} ago",
                // Less than 1 day ago: show hours
                < 86400 => $"{(int)elapsed.TotalHours} hour{((int)elapsed.TotalHours == 1 ? "" : "s")} ago",
                // Less than 7 days ago: show days
                < 604800 => $"{(int)elapsed.TotalDays} day{((int)elapsed.TotalDays == 1 ? "" : "s")} ago",
                // Older than 7 days: show the actual date
                _ => CreatedAt.ToString("MMM d, yyyy")
            };
        }
    }
}
