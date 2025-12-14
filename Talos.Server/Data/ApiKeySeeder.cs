using Talos.Server.Data;
using Talos.Server.Models.Entities;
using Talos.Server.Services;

namespace Talos.Server.Data
{
    public static class ApiKeySeeder
    {
        public static void Seed(AppDbContext db)
        {
            if (db.ApiKeys.Any()) return; // Evitar duplicados

            var owners = new[] { "sergio", "deivis", "sarah", "miguel", "builes" };
            foreach (var owner in owners)
            {
                var key = new ApiKey
                {
                    Key = ApiKeyGenerator.GenerateKey(),
                    Owner = owner,
                    Role = "admin",
                    ExpiresAt = DateTime.UtcNow.AddYears(1),
                    IsActive = true
                };
                db.ApiKeys.Add(key);
            }

            db.SaveChanges();
        }
    }
}