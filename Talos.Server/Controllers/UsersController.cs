using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Talos.Server.Data;
using Talos.Server.Models;

namespace Talos.Server.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<UsersController> _logger;

    public UsersController(AppDbContext context, IDistributedCache cache, ILogger<UsersController> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    // GET: api/users/{id}/templates
    [HttpGet("{id}/templates")]
    public async Task<IActionResult> GetUserTemplates(int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool? isPublic = null)
    {
        try
        {
            // Cache key basado en parámetros
            string cacheKey = $"user_{id}_templates_page{page}_size{pageSize}_public{isPublic}";
            string cachedData = await _cache.GetStringAsync(cacheKey);

            if (cachedData != null)
            {
                var cachedResult = JsonSerializer.Deserialize<object>(cachedData);
                return Ok(new { source = "cache", data = cachedResult });
            }

            // Verificar si el usuario existe
            var userExists = await _context.Users.AnyAsync(u => u.Id == id);
            if (!userExists)
            {
                return NotFound(new { message = "Usuario no encontrado" });
            }

            // Construir consulta con los nombres correctos
            var query = _context.Templates
                .Where(t => t.UserId == id)
                .OrderByDescending(t => t.CreateAt)
                .AsQueryable();

            // Filtrar por visibilidad si se especifica
            if (isPublic.HasValue)
            {
                query = query.Where(t => t.IsPublic == isPublic.Value);
            }

            // Paginación
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Obtener datos paginados
            var templates = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new
                {
                    id = t.Id,
                    name = t.TemplateName,
                    slug = t.Slug,
                    isPublic = t.IsPublic,
                    license = t.LicenseType,
                    createdAt = t.CreateAt,
                    dependenciesCount = t.TemplateDependencies != null ? t.TemplateDependencies.Count : 0,
                    userId = t.UserId
                })
                .ToListAsync();

            var result = new
            {
                userId = id,
                page,
                pageSize,
                totalItems,
                totalPages,
                templates
            };

            // Cachear resultados
            var serialized = JsonSerializer.Serialize(result);
            await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            return Ok(new { source = "database", data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error obteniendo templates del usuario {id}");
            return StatusCode(500, new { message = "Error interno", detail = ex.Message });
        }
    }

    // GET: api/users/{id}/stats
    [HttpGet("{id}/stats")]
    public async Task<IActionResult> GetUserStats(int id)
    {
        try
        {
            string cacheKey = $"user_{id}_stats";
            string cachedData = await _cache.GetStringAsync(cacheKey);

            if (cachedData != null)
            {
                var cachedResult = JsonSerializer.Deserialize<object>(cachedData);
                return Ok(new { source = "cache", data = cachedResult });
            }

            // Verificar si el usuario existe
            var user = await _context.Users
                .Include(u => u.Templates)
                .ThenInclude(t => t.TemplateDependencies)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound(new { message = "Usuario no encontrado" });
            }

            // Obtener estadísticas de followers/following
            var followersCount = await _context.Follows
                .CountAsync(f => f.FollowedUserId == id);

            var followingCount = await _context.Follows
                .CountAsync(f => f.FollowingUserId == id);

            var templates = user.Templates ?? new List<Template>();

            var stats = new
            {
                userId = user.Id,
                username = user.Username,
                email = user.Email,
                role = user.Role,
                createdAt = user.CreatedAt,
                
                // Estadísticas de templates
                totalTemplates = templates.Count,
                publicTemplates = templates.Count(t => t.IsPublic),
                privateTemplates = templates.Count(t => !t.IsPublic),
                
                // Estadísticas de dependencias
                totalDependencies = templates.Sum(t => t.TemplateDependencies?.Count ?? 0),
                averageDependenciesPerTemplate = templates.Any()
                    ? Math.Round(templates.Average(t => t.TemplateDependencies?.Count ?? 0), 2)
                    : 0,
                
                // Fechas
                newestTemplateDate = templates.Any()
                    ? templates.Max(t => t.CreateAt)
                    : (DateTime?)null,
                oldestTemplateDate = templates.Any()
                    ? templates.Min(t => t.CreateAt)
                    : (DateTime?)null,
                
                // Estadísticas sociales
                followersCount,
                followingCount,
                postsCount = user.Posts?.Count ?? 0,
                
                // Licencias más usadas
                topLicenses = templates
                    .GroupBy(t => t.LicenseType)
                    .Select(g => new { license = g.Key, count = g.Count() })
                    .OrderByDescending(x => x.count)
                    .Take(5)
                    .ToList(),
                
                // Última actividad
                lastActive = templates.Any()
                    ? templates.Max(t => t.CreateAt)
                    : user.CreatedAt,
                
                // Templates por mes (últimos 6 meses)
                templatesLast6Months = Enumerable.Range(0, 6)
                    .Select(i => DateTime.UtcNow.AddMonths(-i))
                    .Select(month => new
                    {
                        month = month.ToString("yyyy-MM"),
                        count = templates.Count(t => t.CreateAt.Year == month.Year && t.CreateAt.Month == month.Month)
                    })
                    .Where(x => x.count > 0)
                    .OrderBy(x => x.month)
                    .ToList()
            };

            // Cachear estadísticas por más tiempo
            var serialized = JsonSerializer.Serialize(stats);
            await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
            });

            return Ok(new { source = "database", data = stats });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error obteniendo estadísticas del usuario {id}");
            return StatusCode(500, new { message = "Error interno", detail = ex.Message });
        }
    }

    // GET: api/users/{id} (información básica del usuario)
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        try
        {
            string cacheKey = $"user_{id}_info";
            string cachedData = await _cache.GetStringAsync(cacheKey);

            if (cachedData != null)
            {
                var cachedResult = JsonSerializer.Deserialize<object>(cachedData);
                return Ok(new { source = "cache", data = cachedResult });
            }

            var user = await _context.Users
                .Where(u => u.Id == id)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Role,
                    u.CreatedAt,
                    templateCount = u.Templates.Count,
                    followersCount = _context.Follows.Count(f => f.FollowedUserId == u.Id),
                    followingCount = _context.Follows.Count(f => f.FollowingUserId == u.Id),
                    postsCount = u.Posts.Count,
                    isActive = u.Templates.Any() || u.Posts.Any(),
                    lastTemplateDate = u.Templates.Any()
                        ? u.Templates.Max(t => t.CreateAt)
                        : (DateTime?)null,
                    lastPostDate = u.Posts.Any()
                        ? u.Posts.Max(p => p.CreatedAt)
                        : (DateTime?)null
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new { message = "Usuario no encontrado" });
            }

            var serialized = JsonSerializer.Serialize(user);
            await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });

            return Ok(new { source = "database", data = user });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error obteniendo usuario {id}");
            return StatusCode(500, new { message = "Error interno", detail = ex.Message });
        }
    }

    // GET: api/users/search?q={query} (búsqueda de usuarios)
    [HttpGet("search")]
    public async Task<IActionResult> SearchUsers([FromQuery] string q, [FromQuery] int limit = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return BadRequest(new { message = "Término de búsqueda muy corto (mínimo 2 caracteres)" });
            }

            string cacheKey = $"users_search_{q.ToLower()}_{limit}";
            string cachedData = await _cache.GetStringAsync(cacheKey);

            if (cachedData != null)
            {
                var cachedResult = JsonSerializer.Deserialize<object>(cachedData);
                return Ok(new { source = "cache", data = cachedResult });
            }

            var users = await _context.Users
                .Where(u => u.Username.Contains(q) || u.Email.Contains(q))
                .OrderBy(u => u.Username)
                .Take(limit)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Role,
                    templateCount = u.Templates.Count,
                    followersCount = _context.Follows.Count(f => f.FollowedUserId == u.Id),
                    createdAt = u.CreatedAt,
                    lastActive = u.Templates.Any() 
                        ? u.Templates.Max(t => t.CreateAt) 
                        : u.CreatedAt
                })
                .ToListAsync();

            var serialized = JsonSerializer.Serialize(users);
            await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            return Ok(new { source = "database", data = users });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error buscando usuarios con término: {q}");
            return StatusCode(500, new { message = "Error interno", detail = ex.Message });
        }
    }

    // GET: api/users/{id}/followers (lista de seguidores)
    [HttpGet("{id}/followers")]
    public async Task<IActionResult> GetUserFollowers(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            string cacheKey = $"user_{id}_followers_page{page}_size{pageSize}";
            string cachedData = await _cache.GetStringAsync(cacheKey);

            if (cachedData != null)
            {
                var cachedResult = JsonSerializer.Deserialize<object>(cachedData);
                return Ok(new { source = "cache", data = cachedResult });
            }

            var followers = await _context.Follows
                .Where(f => f.FollowedUserId == id)
                .Include(f => f.FollowingUser)
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new
                {
                    userId = f.FollowingUser.Id,
                    username = f.FollowingUser.Username,
                    email = f.FollowingUser.Email,
                    followedAt = f.CreatedAt,
                    templateCount = f.FollowingUser.Templates.Count
                })
                .ToListAsync();

            var totalFollowers = await _context.Follows.CountAsync(f => f.FollowedUserId == id);
            var totalPages = (int)Math.Ceiling(totalFollowers / (double)pageSize);

            var result = new
            {
                userId = id,
                page,
                pageSize,
                totalFollowers,
                totalPages,
                followers
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
            _logger.LogError(ex, $"Error obteniendo seguidores del usuario {id}");
            return StatusCode(500, new { message = "Error interno", detail = ex.Message });
        }
    }

    // GET: api/users/{id}/following (lista de usuarios seguidos)
    [HttpGet("{id}/following")]
    public async Task<IActionResult> GetUserFollowing(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            string cacheKey = $"user_{id}_following_page{page}_size{pageSize}";
            string cachedData = await _cache.GetStringAsync(cacheKey);

            if (cachedData != null)
            {
                var cachedResult = JsonSerializer.Deserialize<object>(cachedData);
                return Ok(new { source = "cache", data = cachedResult });
            }

            var following = await _context.Follows
                .Where(f => f.FollowingUserId == id)
                .Include(f => f.FollowedUser)
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new
                {
                    userId = f.FollowedUser.Id,
                    username = f.FollowedUser.Username,
                    email = f.FollowedUser.Email,
                    followedAt = f.CreatedAt,
                    templateCount = f.FollowedUser.Templates.Count
                })
                .ToListAsync();

            var totalFollowing = await _context.Follows.CountAsync(f => f.FollowingUserId == id);
            var totalPages = (int)Math.Ceiling(totalFollowing / (double)pageSize);

            var result = new
            {
                userId = id,
                page,
                pageSize,
                totalFollowing,
                totalPages,
                following
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
            _logger.LogError(ex, $"Error obteniendo usuarios seguidos por {id}");
            return StatusCode(500, new { message = "Error interno", detail = ex.Message });
        }
    }
}