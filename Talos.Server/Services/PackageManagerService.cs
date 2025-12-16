using Microsoft.EntityFrameworkCore;
using Talos.Server.Data;
using Talos.Server.Models;

namespace Talos.Server.Services;

public class PackageManagerService
{
    private readonly AppDbContext _context;

    public PackageManagerService(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<PackageManager?> GetByNameAsync(string name)
    {
        return await _context.PackageManagers
            .AsNoTracking()
            .Include(m => m.Packages)
            .FirstOrDefaultAsync(m => m.Name == name);
    }
}