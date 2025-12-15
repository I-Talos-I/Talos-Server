using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Talos.Server.Data;
using Talos.Server.Models;
using OperatingSystem = Talos.Server.Models.Entities.OperatingSystem;

namespace Talos.Server.Controllers.V1;

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

    [HttpGet("{userId:int}/{slug}.json")]
    public async Task<IActionResult> GetRegistry(
        int userId,
        string slug)
    {
        var template = await _db.Templates
            .AsNoTracking()
            .Include(t => t.User)
            .Include(t => t.Dependencies)
                .ThenInclude(d => d.Versions)
            .Include(t => t.Dependencies)
                .ThenInclude(d => d.Commands)
            .FirstOrDefaultAsync(t =>
                t.UserId == userId &&
                t.Slug == slug &&
                !t.IsPublic
            );

        if (template == null)
            return NotFound();

        var response = new
        {
            schema = "https://schemas.aodesu.com/talos/package.json",
            name = template.Slug,
            author = template.User?.Username,
            description = template.Name,
            dependencies = template.Dependencies.Select(d => new
            {
                name = d.Name,
                version = d.Versions
                    .OrderBy(v => v.Id)
                    .Select(v => v.Version)
                    .ToArray(),
                commands = new
                {
                    linux = d.Commands
                        .Where(c => c.OS == OperatingSystem.Linux)
                        .OrderBy(c => c.Order)
                        .Select(c => c.Command)
                        .ToArray(),

                    windows = d.Commands
                        .Where(c => c.OS == OperatingSystem.Windows)
                        .OrderBy(c => c.Order)
                        .Select(c => c.Command)
                        .ToArray(),

                    macos = d.Commands
                        .Where(c => c.OS == OperatingSystem.MacOS)
                        .OrderBy(c => c.Order)
                        .Select(c => c.Command)
                        .ToArray()
                }
            })
        };

        return Ok(response);
    }
}