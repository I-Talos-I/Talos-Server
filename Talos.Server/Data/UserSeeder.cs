using Microsoft.EntityFrameworkCore;
using Talos.Server.Models;

namespace Talos.Server.Data;

public class UserSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Users.AnyAsync(u => u.Role == "admin"))
        {
            var admin = new User
            {
                Username = "admin",
                Email = "Admin@talos.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("BuyTerrariaForCami!"), // contraseña inicial
                Role = "admin",
                CreatedAt = DateTime.UtcNow
            };

            await db.Users.AddAsync(admin);
            await db.SaveChangesAsync();
        }
    }
}