using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public interface INotificationService
{
    Task<List<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false);
    Task<Notification> CreateNotificationAsync(Notification notification);
    Task<bool> MarkAsReadAsync(int notificationId, int requestingUserId);
    Task<int> GetUnreadCountAsync(int userId);
    Task NotifyDocumentSharedAsync(Document document, int recipientUserId, string actorName);
    Task NotifyProjectDocumentAddedAsync(Document document, IEnumerable<int> recipientUserIds, string actorName);
}

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        return await query
            .OrderByDescending(n => n.Priority)
            .ThenByDescending(n => n.CreatedDate)
            .Take(50)
            .ToListAsync();
    }

    public async Task<Notification> CreateNotificationAsync(Notification notification)
    {
        notification.CreatedDate = DateTime.UtcNow;
        notification.IsRead = false;

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        return notification;
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, int requestingUserId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification == null) return false;

        // Authorization: Users can only mark their own notifications as read
        if (notification.UserId != requestingUserId)
        {
            return false; // User not authorized to mark this notification as read
        }

        notification.IsRead = true;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task NotifyDocumentSharedAsync(Document document, int recipientUserId, string actorName)
    {
        await CreateNotificationAsync(new Notification
        {
            UserId = recipientUserId,
            Title = "Document shared with you",
            Message = $"{actorName} shared '{document.Title}' with you.",
            Type = NotificationType.DocumentShared
        });
    }

    public async Task NotifyProjectDocumentAddedAsync(Document document, IEnumerable<int> recipientUserIds, string actorName)
    {
        foreach (var userId in recipientUserIds.Distinct())
            await CreateNotificationAsync(new Notification
            {
                UserId = userId,
                Title = "New project document",
                Message = $"{actorName} added '{document.Title}' to your project.",
                Type = NotificationType.ProjectDocumentAdded
            });
    }
}
