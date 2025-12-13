using Microsoft.EntityFrameworkCore;
using Talos.Server.Data;
using Talos.Server.Services.Interfaces;

namespace Talos.Server.Middleware
{
    public class UserActivityMiddleware
    {
        private readonly RequestDelegate _next;

        public UserActivityMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext db, IUserStatusService statusService)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out int userId))
                {
                    var user = await db.Users.FindAsync(userId);
                    if (user != null)
                    {
                        // Actualizar último visto
                        user.LastSeenAt = DateTime.UtcNow;
                        user.IsOnline = true;

                        // Actualizar cache/estado online
                        await statusService.SetUserOnlineAsync(userId);

                        await db.SaveChangesAsync();
                    }
                }
            }

            await _next(context);
        }
    }
}