using Microsoft.EntityFrameworkCore;
using Talos.Server.Data;
using Talos.Server.Models;
using Talos.Server.Models.Dtos;
using Talos.Server.Services.Interfaces;

namespace Talos.Server.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;

    public NotificationService(AppDbContext context)
    {
        _context = context;
    }

    // Crear notificación
    public async Task<NotificationDto> CreateAsync(
        int userId,
        string title,
        string message,
        int? tagId = null
    )
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            TagId = tagId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        return await MapToDtoAsync(notification.Id);
    }

    // Obtener notificaciones del usuario
    public async Task<List<NotificationDto>> GetUserNotificationsAsync(
        int userId,
        bool unreadOnly = false
    )
    {
        var query = _context.Notifications
            .Include(n => n.Tag)
            .Where(n => n.UserId == userId);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                TagId = n.TagId,
                TagName = n.Tag != null ? n.Tag.Name : null
            })
            .ToListAsync();
    }

    // ✔️ Marcar una notificación como leída (seguro)
    public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null)
            return false;

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }

        return true;
    }

    // Marcar todas como leídas
    public async Task MarkAllAsReadAsync(int userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var n in notifications)
            n.IsRead = true;

        await _context.SaveChangesAsync();
    }

    // Helper privado
    private async Task<NotificationDto> MapToDtoAsync(int notificationId)
    {
        return await _context.Notifications
            .Include(n => n.Tag)
            .Where(n => n.Id == notificationId)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                TagId = n.TagId,
                TagName = n.Tag != null ? n.Tag.Name : null
            })
            .FirstAsync();
    }
}
