using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore;
using Talos.Server.Data;
using Talos.Server.Models.Dtos;
using Talos.Server.Services.Interfaces;

namespace Talos.Server.Services
{
    public class UserStatusService : IUserStatusService
    {
        private readonly IDistributedCache _cache;
        private readonly AppDbContext _context;

        // Tiempo que consideramos al usuario "online"
        private const int ONLINE_TTL_MINUTES = 5;

        public UserStatusService(IDistributedCache cache, AppDbContext context)
        {
            _cache = cache;
            _context = context;
        }

        // Genera la clave para Redis/Cache
        private static string GetKey(int userId) => $"user:online:{userId}";

        // Marcar usuario como online
        public async Task SetUserOnlineAsync(int userId)
        {
            await _cache.SetStringAsync(
                GetKey(userId),
                "true",
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(ONLINE_TTL_MINUTES)
                });
        }

        // Marcar usuario como offline
        public async Task SetUserOfflineAsync(int userId)
        {
            await _cache.RemoveAsync(GetKey(userId));
        }

        // Verificar si usuario está online
        public async Task<bool> IsUserOnlineAsync(int userId)
        {
            var value = await _cache.GetStringAsync(GetKey(userId));
            return value != null;
        }

        // Obtener estado de todos los usuarios
        public async Task<List<UserStatusDto>> GetUsersStatusAsync()
        {
            // Traer solo id y username desde DB
            var users = await _context.Users
                .Select(u => new { u.Id, u.UserName })
                .ToListAsync();

            var result = new List<UserStatusDto>();

            // Verificar online en cache
            foreach (var u in users)
            {
                result.Add(new UserStatusDto
                {
                    UserId = u.Id,
                    Username = u.UserName,
                    IsOnline = await IsUserOnlineAsync(u.Id)
                });
            }

            return result;
        }
    }
}
