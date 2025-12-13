using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Talos.Server.Data;
using Talos.Server.Models;
using Talos.Server.Models.Dtos.Package;

namespace Talos.Server.Controllers;

[ApiController]
[Route("api/packages")]
public class PackagesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<PackagesController> _logger;
    private readonly IMapper _mapper;

    public PackagesController(
        AppDbContext context,
        IMapper mapper,
        ILogger<PackagesController> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    // GET: api/packages (pagination + filters)
    [HttpGet]
    public async Task<IActionResult> GetAllPackages(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? manager = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var query = _context.Packages
                .Include(p => p.PackageManager)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(manager))
                query = query.Where(p => p.PackageManager.Name == manager);

            if (isActive.HasValue)
                query = query.Where(p => p.IsActive == isActive);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var packages = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.ShortName,
                    manager = p.PackageManager == null ? null : new
                    {
                        p.PackageManager.Id,
                        p.PackageManager.Name
                    },
                    p.RepositoryUrl,
                    p.OfficialDocumentationUrl,
                    p.IsActive,
                    versionsCount = p.PackageVersions.Count,
                    dependenciesCount = p.TemplateDependencies.Count,
                    lastUpdated = p.UpdateAt,
                    lastScraped = p.LastScrapedAt
                })
                .ToListAsync();

            return Ok(new
            {
                page,
                pageSize,
                totalItems,
                totalPages,
                packages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo paquetes");
            return StatusCode(500, new { message = "Error interno", detail = ex.Message });
        }
    }

    // GET: api/packages/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetPackageById(int id)
    {
        try
        {
            var package = await _context.Packages
                .Include(p => p.PackageManager)
                .Include(p => p.PackageVersions)
                .Include(p => p.TemplateDependencies)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (package == null)
                return NotFound(new { message = "Paquete no encontrado" });

            return Ok(new
            {
                package.Id,
                package.Name,
                package.ShortName,
                manager = package.PackageManager == null ? null : new
                {
                    package.PackageManager.Id,
                    package.PackageManager.Name
                },
                package.RepositoryUrl,
                package.OfficialDocumentationUrl,
                package.IsActive,
                package.CreateAt,
                package.UpdateAt,
                package.LastScrapedAt,
                versions = package.PackageVersions
                    .OrderByDescending(v => v.ReleaseDate)
                    .Select(v => new
                    {
                        v.Id,
                        v.Version,
                        v.ReleaseDate,
                        v.IsDeprecated,
                        v.DeprecationMessage,
                        v.DownloadUrl,
                        v.ReleaseNotesUrl
                    }),
                stats = new
                {
                    totalVersions = package.PackageVersions.Count,
                    stableVersions = package.PackageVersions.Count(v => !v.IsDeprecated),
                    deprecatedVersions = package.PackageVersions.Count(v => v.IsDeprecated),
                    usedInTemplates = package.TemplateDependencies.Count
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error obteniendo paquete {id}");
            return StatusCode(500, new { message = "Error interno", detail = ex.Message });
        }
    }

    // POST: api/packages
    [HttpPost]
    public async Task<IActionResult> CreatePackage([FromBody] PackageCreateDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!await _context.PackageManagers.AnyAsync(m => m.Id == dto.PackageManagerId))
                return NotFound(new { message = "Gestor de paquetes no existe" });

            var package = _mapper.Map<Package>(dto);
            package.CreateAt = DateTime.UtcNow;
            package.UpdateAt = DateTime.UtcNow;

            _context.Packages.Add(package);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPackageById),
                new { id = package.Id },
                new { message = "Paquete creado correctamente", package });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando paquete");
            return StatusCode(500, new { message = "Error interno", detail = ex.Message });
        }
    }

    // PUT: api/packages/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdatePackage(int id, [FromBody] PackageCreateDto dto)
    {
        try
        {
            var package = await _context.Packages.FindAsync(id);
            if (package == null)
                return NotFound(new { message = "Paquete no encontrado" });

            if (!await _context.PackageManagers.AnyAsync(m => m.Id == dto.PackageManagerId))
                return NotFound(new { message = "Gestor de paquetes no válido" });

            _mapper.Map(dto, package);
            package.UpdateAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Paquete actualizado correctamente", package });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error actualizando paquete {id}");
            return StatusCode(500, new { message = "Error interno", detail = ex.Message });
        }
    }

    // DELETE: api/packages/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePackage(int id)
    {
        try
        {
            var package = await _context.Packages
                .Include(p => p.PackageVersions)
                .Include(p => p.TemplateDependencies)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (package == null)
                return NotFound(new { message = "Paquete no encontrado" });

            if (package.TemplateDependencies.Any())
                return BadRequest(new
                {
                    message = "No se puede eliminar el paquete porque está en uso",
                    usedBy = package.TemplateDependencies.Count
                });

            _context.PackageVersions.RemoveRange(package.PackageVersions);
            _context.Packages.Remove(package);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Paquete eliminado correctamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error eliminando paquete {id}");
            return StatusCode(500, new { message = "Error interno", detail = ex.Message });
        }
    }

    // GET: api/packages/by-manager/{managerId}
    [HttpGet("by-manager/{managerId:int}")]
    public async Task<IActionResult> GetAllByManager(int managerId)
    {
        try
        {
            var manager = await _context.PackageManagers.FindAsync(managerId);
            if (manager == null)
                return NotFound(new { message = "Gestor de paquetes no encontrado" });

            var packages = await _context.Packages
                .Where(p => p.PackageManagerId == managerId)
                .OrderBy(p => p.Name)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.ShortName,
                    p.RepositoryUrl,
                    p.IsActive,
                    versionCount = p.PackageVersions.Count
                })
                .ToListAsync();

            return Ok(new
            {
                manager = new { manager.Id, manager.Name },
                total = packages.Count,
                packages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error obteniendo paquetes del gestor {managerId}");
            return StatusCode(500, new { message = "Error interno", detail = ex.Message });
        }
    }
}
