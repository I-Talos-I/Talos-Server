using Microsoft.EntityFrameworkCore;
using Talos.Server.Data;


namespace Talos.Server.Services;

public class TemplateService
{
    private readonly AppDbContext _db;

    public TemplateService(AppDbContext db)
    {
        _db = db;
    }

    // BASE: for ID 
    public async Task<Template?> GetByIdAsync(int id)
    {
        return await _db.Templates
            .Include(t => t.User)
            .Include(t => t.TemplateDependencies)
            .ThenInclude(d => d.Package)
            .Include(t => t.TemplateDependencies)
            .ThenInclude(d => d.VersionConstraint)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    // BASE: per user + slug (REGISTRY)
    public async Task<Template?> GetByUserAndSlugAsync(int userId, string slug)
    {
        return await _db.Templates
            .Include(t => t.User)
            .Include(t => t.TemplateDependencies)
            .ThenInclude(d => d.Package)
            .Include(t => t.TemplateDependencies)
            .ThenInclude(d => d.VersionConstraint)
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.UserId == userId &&
                t.Slug == slug &&
                !t.IsPublic
            );
    }

// BASE: crear plantilla para POST
    public async Task<Template> CreateAsync(Template template)
    {
        _db.Templates.Add(template);
        await _db.SaveChangesAsync();
        return template;
    }
}