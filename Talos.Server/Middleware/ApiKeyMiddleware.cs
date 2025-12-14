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
            var path = context.Request.Path.Value?.ToLower();

            if (path.StartsWith("/api/v1/auth/login") ||
                path.StartsWith("/health"))
            {
                await _next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue("x-api-key", out var extractedApiKey))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("API Key missing");
                return;
            }

            var apiKeyValue = extractedApiKey.ToString();

            var apiKey = await db.ApiKeys
                .AsNoTracking()
                .FirstOrDefaultAsync(k =>
                    k.Key == apiKeyValue &&
                    k.IsActive &&
                    (k.ExpiresAt == null || k.ExpiresAt > DateTime.UtcNow)
                );

            if (apiKey == null)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Invalid or expired API Key");
                return;
            }

            await _next(context);
        }

    }
}