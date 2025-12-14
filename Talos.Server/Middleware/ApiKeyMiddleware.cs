using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Talos.Server.Data;
using System.Threading.Tasks;

namespace Talos.Server.Middleware
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiKeyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext db)
        {
            // Rutas que no requieren API Key
            var path = context.Request.Path.Value?.ToLower();
            if (path.StartsWith("/api/v1/auth/login") ||
                path.StartsWith("/health"))
            {
                await _next(context);
                return;
            }

            // Revisar header x-api-key
            if (!context.Request.Headers.TryGetValue("x-api-key", out var extractedApiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("API Key missing");
                return;
            }

            var apiKey = await db.ApiKeys
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.Key == extractedApiKey && k.IsActive && 
                                          (k.ExpiresAt == null || k.ExpiresAt > DateTime.UtcNow));

            if (apiKey == null)
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Invalid or expired API Key");
                return;
            }

            // Continuar con la solicitud
            await _next(context);
        }
    }
}