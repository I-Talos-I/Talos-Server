using Talos.Server.Models.Dtos;

namespace Talos.Server.Services.Interfaces;

public interface INotificationService
{
    Task<NotificationDto> CreateAsync(
        int userId,
        string title,
        string message,
        int? tagId = null
    );

    Task<List<NotificationDto>> GetUserNotificationsAsync(
        int userId,
        bool unreadOnly = false
    );

    Task<bool> MarkAsReadAsync(
        int notificationId,
        int userId
    );

    Task MarkAllAsReadAsync(
        int userId
    );
}