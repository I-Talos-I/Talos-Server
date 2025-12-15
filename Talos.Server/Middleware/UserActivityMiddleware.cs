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

        public async Task InvokeAsync(HttpContext context, IUserStatusService statusService)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = context.User
                    .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (int.TryParse(userIdClaim, out int userId))
                {
                    // SOLO cache (rápido, sin DB)
                    await statusService.SetUserOnlineAsync(userId);
                }
            }

            await _next(context);
        }
    }
}