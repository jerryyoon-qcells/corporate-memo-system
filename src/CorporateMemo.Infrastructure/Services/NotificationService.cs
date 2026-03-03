using CorporateMemo.Application.Interfaces;
using CorporateMemo.Domain.Entities;
using CorporateMemo.Domain.Enums;
using CorporateMemo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CorporateMemo.Infrastructure.Services;

/// <summary>
/// Entity Framework Core implementation of the <see cref="INotificationService"/> interface.
/// Persists in-app notifications to the database and retrieves them for the notification panel.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    /// <summary>Initializes the notification service with the database context.</summary>
    public NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Notification> CreateNotificationAsync(
        string userId,
        string message,
        NotificationType type,
        Guid? memoId = null,
        CancellationToken ct = default)
    {
        // Create a new notification record
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Message = message,
            Type = type,
            IsRead = false,           // All new notifications start as unread
            CreatedAt = DateTime.UtcNow,
            MemoId = memoId
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(ct);

        _logger.LogDebug("Created notification for user {UserId}: {Message}", userId, message);

        return notification;
    }

    /// <inheritdoc/>
    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default)
    {
        // Count only unread notifications for this user
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, ct);
    }

    /// <inheritdoc/>
    public async Task<List<Notification>> GetNotificationsAsync(string userId, int count = 20, CancellationToken ct = default)
    {
        // Retrieve the most recent notifications, newest first, up to the requested count
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(count)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task MarkAllReadAsync(string userId, CancellationToken ct = default)
    {
        // Use ExecuteUpdateAsync for a single bulk UPDATE statement (more efficient than loading all records)
        // This updates the IsRead column directly without loading entities into memory
        var count = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(n => n.IsRead, true),
                ct);

        _logger.LogDebug("Marked {Count} notifications as read for user {UserId}", count, userId);
    }
}
