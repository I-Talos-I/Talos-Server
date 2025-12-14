using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Talos.Server.Data;

[ApiController]
[ApiExplorerSettings(GroupName = "v1")]
[Route("api/v1/r")]
public class RegistryController : ControllerBase
{
    private readonly AppDbContext _db;

    public RegistryController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("{userId}/{slug}.json")]
    public async Task<IActionResult> GetRegistry(
        int userId,
        string slug)
    {
        var template = await _db.Templates
            .Include(t => t.User)
            .Include(t => t.TemplateDependencies)
                .ThenInclude(d => d.Package)
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.UserId == userId &&
                t.Slug == slug &&
                !t.IsPublic
            );

        if (template == null)
            return NotFound();

        var response = new
        {
            template = new
            {
                name = template.TemplateName,
                author = template.User.Username
            },
            dependencies = template.TemplateDependencies.Select(d => new
            {
                package = d.Package.ShortName,
                version = d.VersionConstraint
            }),
            install = new
            {
                windows = "npm install",
                linux = "npm install",
                macos = "npm install"
            }
        };

        return Ok(response);
    }
}
