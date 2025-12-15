using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Talos.Server.Services.Interfaces;

namespace Talos.Server.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private int GetUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    // GET: api/notifications
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications()
    {
        var userId = GetUserId();
        var notifications = await _notificationService.GetUserNotificationsAsync(userId);
        return Ok(notifications);
    }
    
    // GET: api/notifications/unread
    [HttpGet("unread")]
    public async Task<IActionResult> GetUnreadNotifications()
    {
        var userId = GetUserId();

        var notifications = await _notificationService
            .GetUserNotificationsAsync(userId, unreadOnly: true);

        return Ok(notifications);
    }

    // PUT: api/notifications/{id}/read
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetUserId();
        
        var success = await _notificationService.MarkAsReadAsync(id,  userId);
        if (!success)
            return NotFound();
        return NoContent();
    }

    // PUT: api/notifications/read-all
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetUserId();
        await _notificationService.MarkAllAsReadAsync(userId);
        return NoContent();
    }
}