using Microsoft.EntityFrameworkCore;
using Talos.Server.Data;
using Talos.Server.Models;

namespace Talos.Server.Services;

public class TemplateService
{
    private readonly AppDbContext _db;

    public TemplateService(AppDbContext db)
    {
        _db = db;
    }

    // BASE: obtener por ID (vista completa)
    public async Task<Template?> GetByIdAsync(int id)
    {
        return await _db.Templates
            .AsNoTracking()
            .Include(t => t.User)
            .Include(t => t.Dependencies)
            .ThenInclude(d => d.Versions)
            .Include(t => t.Dependencies)
            .ThenInclude(d => d.Commands)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    // BASE: por usuario + slug (privado / registry)
    public async Task<Template?> GetByUserAndSlugAsync(int userId, string slug)
    {
        return await _db.Templates
            .AsNoTracking()
            .Include(t => t.Dependencies)
            .ThenInclude(d => d.Versions)
            .Include(t => t.Dependencies)
            .ThenInclude(d => d.Commands)
            .FirstOrDefaultAsync(t =>
                t.UserId == userId &&
                t.Slug == slug &&
                !t.IsPublic
            );
    }

    // BASE: crear plantilla
    public async Task<Template> CreateAsync(Template template)
    {
        _db.Templates.Add(template);
        await _db.SaveChangesAsync();
        return template;
    }
}