using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Talos.Server.Data;
using Talos.Server.Models;

namespace Talos.Server.Controllers;

[ApiController]
[Route("api/packages")]
public class PackagesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<PackagesController> _logger;
    private readonly IServiceProvider _serviceProvider;

    public PackagesController(
        AppDbContext context, 
        IDistributedCache cache, 
        ILogger<PackagesController> logger,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    // GET: api/packages
    [HttpGet]
    public async Task<IActionResult> GetAllPackages(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? manager = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            // Cache key basada en parámetros
            string cacheKey = $"packages_page{page}_size{pageSize}_manager{manager}_active{isActive}";
            string cachedData = await _cache.GetStringAsync(cacheKey);

            if (cachedData != null)
            {
                var cachedResult = JsonSerializer.Deserialize<object>(cachedData);
                return Ok(new { source = "cache", data = cachedResult });
            }

            // Construir consulta
            var query = _context.Packages
                .Include(p => p.PackageManager)
                .AsQueryable();

            // Aplicar filtros
            if (!string.IsNullOrEmpty(manager))
            {
                query = query.Where(p => p.PackageManager.Name == manager);
            }

            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
            }

            // Paginación
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Obtener datos paginados
            var packages = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.ShortName,
                    manager = p.PackageManager != null ? new
                    {
                        p.PackageManager.Id,
                        p.PackageManager.Name
                    } : null,
                    p.RepositoryUrl,
                    p.OfficialDocumentationUrl,
                    p.IsActive,
                    versionsCount = p.PackageVersions.Count,
                    lastUpdated = p.UpdateAt,
                    dependenciesCount = p.TemplateDependencies.Count,
                    lastScraped = p.LastScrapedAt
                })
                .ToListAsync();

            var result = new
            {
                page,
                pageSize,
                totalItems,
                totalPages,
                packages
            };

            // Cachear resultados
            var serialized = JsonSerializer.Serialize(result);
            await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });

            return Ok(new { source = "database", data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo paquetes");
            return StatusCode(500, new { message = "Error interno", detail = ex.Message });
        }
    }

    // GET: api/packages/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPackageById(int id)
    {
        try
        {
            string cacheKey = $"package_{id}";
            string cachedData = await _cache.GetStringAsync(cacheKey);

            if (cachedData != null)
            {
                var cachedResult = JsonSerializer.Deserialize<object>(cachedData);
                return Ok(new { source = "cache", data = cachedResult });
            }

            var package = await _context.Packages
                .Include(p => p.PackageManager)
                .Include(p => p.PackageVersions)
                .Include(p => p.TemplateDependencies)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (package == null)
            {
                return NotFound(new { message = "Paquete no encontrado" });
            }

            var result = new
            {
                package.Id,
                package.Name,
                package.ShortName,
                manager = package.PackageManager != null ? new
                {
                    package.PackageManager.Id,
                    package.PackageManager.Name
                } : null,
                package.RepositoryUrl,
                package.OfficialDocumentationUrl,
                package.IsActive,
                package.LastScrapedAt,
                package.CreateAt,
                package.UpdateAt,
                versions = package.PackageVersions.Select(v => new
                {
                    v.Id,
                    v.Version,
                    v.ReleaseDate,
                    v.IsDeprecated,
                    v.DeprecationMessage,
                    v.DownloadUrl,
                    v.ReleaseNotesUrl
                }).OrderByDescending(v => v.ReleaseDate).ToList(),
                usedInTemplates = package.TemplateDependencies.Select(td => new
                {
                    templateId = td.TemplateId,
                    versionConstraint = td.VersionConstraint
                }).ToList(),
                stats = new
                {
                    totalVersions = package.PackageVersions.Count,
                    stableVersions = package.PackageVersions.Count(v => !v.IsDeprecated),
                    deprecatedVersions = package.PackageVersions.Count(v => v.IsDeprecated),
                    usedInTemplatesCount = package.TemplateDependencies.Count
                }
            };

            // Cachear por más tiempo
            var serialized = JsonSerializer.Serialize(result);
            await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });

            return Ok(new { source = "database", data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error obteniendo paquete {id}");
            return StatusCode(500, new { message = "Error interno", detail = ex.Message });
        }
    }

    // GET: api/packages/search?q={name}
    [HttpGet("search")]
    public async Task<IActionResult> SearchPackages(
        [FromQuery] string q,
        [FromQuery] int limit = 20,
        [FromQuery] string? manager = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return BadRequest(new { message = "Término de búsqueda muy corto (mínimo 2 caracteres)" });
            }

            string cacheKey = $"packages_search_{q.ToLower()}_limit{limit}_manager{manager}";
            string cachedData = await _cache.GetStringAsync(cacheKey);

            if (cachedData != null)
            {
                var cachedResult = JsonSerializer.Deserialize<object>(cachedData);
                return Ok(new { source = "cache", data = cachedResult });
            }

            var query = _context.Packages
                .Include(p => p.PackageManager)
                .Where(p => p.Name.Contains(q) || p.ShortName.Contains(q))
                .AsQueryable();

            if (!string.IsNullOrEmpty(manager))
            {
                query = query.Where(p => p.PackageManager.Name == manager);
            }

            var packages = await query
                .OrderBy(p => p.Name)
                .Take(limit)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.ShortName,
                    managerName = p.PackageManager != null ? p.PackageManager.Name : null,
                    latestVersion = p.PackageVersions
                        .Where(v => !v.IsDeprecated)
                        .OrderByDescending(v => v.ReleaseDate)
                        .Select(v => v.Version)
                        .FirstOrDefault(),
                    p.IsActive,
                    usedCount = p.TemplateDependencies.Count
                })
                .ToListAsync();

            var serialized = JsonSerializer.Serialize(packages);
            await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            return Ok(new { source = "database", data = packages });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error buscando paquetes con término: {q}");
            return StatusCode(500, new { message = "Error interno", detail = ex.Message });
        }
    }

    // GET: api/packages/manager/{managerId}
    [HttpGet("manager/{managerId}")]
    public async Task<IActionResult> GetPackagesByManager(
        int managerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            string cacheKey = $"packages_manager_{managerId}_page{page}_size{pageSize}";
            string cachedData = await _cache.GetStringAsync(cacheKey);

            if (cachedData != null)
            {
                var cachedResult = JsonSerializer.Deserialize<object>(cachedData);
                return Ok(new { source = "cache", data = cachedResult });
            }

            // Verificar si el gestor existe
            var manager = await _context.PackageManagers.FindAsync(managerId);
            if (manager == null)
            {
                return NotFound(new { message = "Gestor de paquetes no encontrado" });
            }

            var query = _context.Packages
                .Where(p => p.PackageManagerId == managerId)
                .Include(p => p.PackageVersions);

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
                    p.RepositoryUrl,
                    p.IsActive,
                    versionsCount = p.PackageVersions.Count,
                    latestVersion = p.PackageVersions
                        .Where(v => !v.IsDeprecated)
                        .OrderByDescending(v => v.ReleaseDate)
                        .Select(v => v.Version)
                        .FirstOrDefault(),
                    lastUpdated = p.UpdateAt
                })
                .ToListAsync();

            var result = new
            {
                manager = new
                {
                    manager.Id,
                    manager.Name
                },
                page,
                pageSize,
                totalItems,
                totalPages,
                packages
            };

            var serialized = JsonSerializer.Serialize(result);
            await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });

            return Ok(new { source = "database", data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error obteniendo paquetes del gestor {managerId}");
            return StatusCode(500, new { message = "Error interno", detail = ex.Message });
        }
    }

    // POST: api/packages/scrape
    [HttpPost("scrape")]
    public async Task<IActionResult> ForceScrapePackages()
    {
        try
        {
            // Verificar si el usuario es admin
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Token inválido" });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.Role != "admin")
            {
                return Forbid();
            }

            _logger.LogInformation($"Iniciando scraping de paquetes por admin: {user.Username}");
            
            // Simular proceso de scraping
            await Task.Delay(2000);
            
            var scrapedCount = new Random().Next(5, 20);
            var updatedCount = new Random().Next(1, 10);
            
            // Actualizar algunos paquetes con nueva fecha de scraping
            var packagesToUpdate = await _context.Packages
                .OrderBy(p => p.LastScrapedAt)
                .Take(updatedCount)
                .ToListAsync();
            
            foreach (var package in packagesToUpdate)
            {
                package.LastScrapedAt = DateTime.UtcNow;
                package.UpdateAt = DateTime.UtcNow;
            }
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Scraping completado: {scrapedCount} nuevos (simulado), {updatedCount} actualizados");

            // Limpiar cache
            await ClearPackagesCache();

            return Ok(new
            {
                success = true,
                message = "Scraping completado exitosamente",
                stats = new
                {
                    newPackages = scrapedCount,
                    updatedPackages = updatedCount,
                    timestamp = DateTime.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el scraping de paquetes");
            return StatusCode(500, new { message = "Error durante el scraping", detail = ex.Message });
        }
    }

    // GET: api/packages/{packageId}/versions
    [HttpGet("{packageId}/versions")]
    public async Task<IActionResult> GetPackageVersions(
        int packageId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? deprecated = null)
    {
        try
        {
            string cacheKey = $"package_{packageId}_versions_page{page}_size{pageSize}_deprecated{deprecated}";
            string cachedData = await _cache.GetStringAsync(cacheKey);

            if (cachedData != null)
            {
                var cachedResult = JsonSerializer.Deserialize<object>(cachedData);
                return Ok(new { source = "cache", data = cachedResult });
            }

            // Verificar si el paquete existe
            var package = await _context.Packages.FindAsync(packageId);
            if (package == null)
            {
                return NotFound(new { message = "Paquete no encontrado" });
            }

            var query = _context.PackageVersions
                .Where(v => v.PackageId == packageId)
                .AsQueryable();

            if (deprecated.HasValue)
            {
                query = query.Where(v => v.IsDeprecated == deprecated.Value);
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var versions = await query
                .OrderByDescending(v => v.ReleaseDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new
                {
                    v.Id,
                    v.Version,
                    v.ReleaseDate,
                    v.IsDeprecated,
                    v.DeprecationMessage,
                    v.DownloadUrl,
                    v.ReleaseNotesUrl,
                    v.CreateAt
                })
                .ToListAsync();

            var result = new
            {
                packageId,
                packageName = package.Name,
                page,
                pageSize,
                totalItems,
                totalPages,
                versions
            };

            var serialized = JsonSerializer.Serialize(result);
            await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            return Ok(new { source = "database", data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error obteniendo versiones del paquete {packageId}");
            return StatusCode(500, new { message = "Error interno", detail = ex.Message });
        }
    }

    // GET: api/packages/{packageId}/versions/latest
    [HttpGet("{packageId}/versions/latest")]
    public async Task<IActionResult> GetLatestVersion(int packageId)
    {
        try
        {
            string cacheKey = $"package_{packageId}_latest_version";
            string cachedData = await _cache.GetStringAsync(cacheKey);

            if (cachedData != null)
            {
                var cachedResult = JsonSerializer.Deserialize<object>(cachedData);
                return Ok(new { source = "cache", data = cachedResult });
            }

            var version = await _context.PackageVersions
                .Where(v => v.PackageId == packageId && !v.IsDeprecated)
                .OrderByDescending(v => v.ReleaseDate)
                .Select(v => new
                {
                    v.Id,
                    v.Version,
                    v.ReleaseDate,
                    v.DownloadUrl,
                    v.ReleaseNotesUrl,
                    v.CreateAt
                })
                .FirstOrDefaultAsync();

            if (version == null)
            {
                return NotFound(new { message = "No se encontró versión estable del paquete" });
            }

            var serialized = JsonSerializer.Serialize(version);
            await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            return Ok(new { source = "database", data = version });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error obteniendo última versión del paquete {packageId}");
            return StatusCode(500, new { message = "Error interno", detail = ex.Message });
        }
    }

    // GET: api/packages/{packageId}/versions/stable
    [HttpGet("{packageId}/versions/stable")]
    public async Task<IActionResult> GetStableVersions(
        int packageId,
        [FromQuery] int limit = 10)
    {
        try
        {
            string cacheKey = $"package_{packageId}_stable_versions_limit{limit}";
            string cachedData = await _cache.GetStringAsync(cacheKey);

            if (cachedData != null)
            {
                var cachedResult = JsonSerializer.Deserialize<object>(cachedData);
                return Ok(new { source = "cache", data = cachedResult });
            }

            var versions = await _context.PackageVersions
                .Where(v => v.PackageId == packageId && !v.IsDeprecated)
                .OrderByDescending(v => v.ReleaseDate)
                .Take(limit)
                .Select(v => new
                {
                    v.Id,
                    v.Version,
                    v.ReleaseDate,
                    v.DownloadUrl,
                    v.ReleaseNotesUrl,
                    daysSinceRelease = (DateTime.UtcNow - v.ReleaseDate).Days
                })
                .ToListAsync();

            if (!versions.Any())
            {
                return NotFound(new { message = "No se encontraron versiones estables del paquete" });
            }

            var serialized = JsonSerializer.Serialize(versions);
            await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            return Ok(new { source = "database", data = versions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error obteniendo versiones estables del paquete {packageId}");
            return StatusCode(500, new { message = "Error interno", detail = ex.Message });
        }
    }

    // Método auxiliar para limpiar cache
    private async Task ClearPackagesCache()
    {
        var tasks = new List<Task>
        {
            _cache.RemoveAsync("packages_"),
            _cache.RemoveAsync("packages_search_"),
            _cache.RemoveAsync("packages_manager_"),
            _cache.RemoveAsync("package_")
        };
        
        await Task.WhenAll(tasks);
    }
}