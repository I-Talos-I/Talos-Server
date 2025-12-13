using Talos.Server.Models;

namespace Talos.Server.Services.Interfaces;

public interface INotificationService
{
    Task<Notification> CreateAsync(
        int userId,
        string title,
        string message,
        int? tagId = null
    );

    Task<List<Notification>> GetUserNotificationsAsync(int userId);

    Task MarkAsReadAsync(int notificationId);
    Task MarkAllAsReadAsync(int userId);
}