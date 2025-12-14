using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Talos.Server.Data;
using Talos.Server.Models;

namespace Talos.Server.Controllers;
[Authorize(Roles = "admin,user")]

[ApiController]
[Route("api/v1/users")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<UsersController> _logger;

    public UsersController(AppDbContext context, ILogger<UsersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ------------------------------------------------------------------
    // 1️⃣ GET /api/v1/users/{id}
    // Información básica del usuario
    // ------------------------------------------------------------------
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        try
        {
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
                    lastTemplateDate = u.Templates.Any()
                        ? u.Templates.Max(t => t.CreateAt)
                        : (DateTime?)null
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { message = "Usuario no encontrado" });

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo usuario");
            return StatusCode(500, new { message = "Error interno" });
        }
    }

    // ------------------------------------------------------------------
    // 2️⃣ GET /api/v1/users/{id}/templates
    // Templates del usuario
    // ------------------------------------------------------------------
    [HttpGet("{id}/templates")]
    public async Task<IActionResult> GetUserTemplates(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool? isPublic = null)
    {
        try
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == id);
            if (!userExists)
                return NotFound(new { message = "Usuario no encontrado" });

            var query = _context.Templates
                .Where(t => t.UserId == id)
                .OrderByDescending(t => t.CreateAt)
                .AsQueryable();

            if (isPublic.HasValue)
                query = query.Where(t => t.IsPublic == isPublic.Value);

            var totalItems = await query.CountAsync();

            var templates = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new
                {
                    t.Id,
                    name = t.TemplateName,
                    t.Slug,
                    t.IsPublic,
                    t.LicenseType,
                    t.CreateAt,
                    dependenciesCount = t.TemplateDependencies.Count
                })
                .ToListAsync();

            return Ok(new
            {
                userId = id,
                page,
                pageSize,
                totalItems,
                templates
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo templates");
            return StatusCode(500, new { message = "Error interno" });
        }
    }

    // ------------------------------------------------------------------
    // 3️⃣ GET /api/v1/users/{id}/stats
    // Estadísticas del usuario
    // ------------------------------------------------------------------
    [HttpGet("{id}/stats")]
    public async Task<IActionResult> GetUserStats(int id)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.Templates)
                    .ThenInclude(t => t.TemplateDependencies)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound(new { message = "Usuario no encontrado" });

            var templates = user.Templates;

            return Ok(new
            {
                user.Id,
                user.Username,
                user.Email,
                user.Role,
                user.CreatedAt,

                totalTemplates = templates.Count,
                publicTemplates = templates.Count(t => t.IsPublic),
                privateTemplates = templates.Count(t => !t.IsPublic),

                totalDependencies = templates.Sum(t => t.TemplateDependencies.Count),
                avgDependencies = templates.Any()
                    ? Math.Round(templates.Average(t => t.TemplateDependencies.Count), 2)
                    : 0,

                followersCount = _context.Follows.Count(f => f.FollowedUserId == id),
                followingCount = _context.Follows.Count(f => f.FollowingUserId == id)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo stats");
            return StatusCode(500, new { message = "Error interno" });
        }
    }

    // ------------------------------------------------------------------
    // 4️⃣ GET /api/v1/users/search?q=
    // Buscar usuarios
    // ------------------------------------------------------------------
    [HttpGet("search")]
    public async Task<IActionResult> SearchUsers(
        [FromQuery] string q,
        [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return BadRequest(new { message = "Mínimo 2 caracteres" });

        var users = await _context.Users
            .Where(u => u.Username.Contains(q) || u.Email.Contains(q))
            .OrderBy(u => u.Username)
            .Take(limit)
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                templateCount = u.Templates.Count
            })
            .ToListAsync();

        return Ok(users);
    }

    // ------------------------------------------------------------------
    // 5️⃣ GET /api/v1/users/{id}/followers
    // Seguidores
    // ------------------------------------------------------------------
    [HttpGet("{id}/followers")]
    public async Task<IActionResult> GetUserFollowers(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var followers = await _context.Follows
            .Where(f => f.FollowedUserId == id)
            .Include(f => f.FollowingUser)
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new
            {
                f.FollowingUser.Id,
                f.FollowingUser.Username,
                followedAt = f.CreatedAt
            })
            .ToListAsync();

        return Ok(new { userId = id, followers });
    }

    // ------------------------------------------------------------------
    // 6️⃣ GET /api/v1/users/{id}/following
    // Usuarios seguidos
    // ------------------------------------------------------------------
    [HttpGet("{id}/following")]
    public async Task<IActionResult> GetUserFollowing(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var following = await _context.Follows
            .Where(f => f.FollowingUserId == id)
            .Include(f => f.FollowedUser)
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new
            {
                f.FollowedUser.Id,
                f.FollowedUser.Username,
                followedAt = f.CreatedAt
            })
            .ToListAsync();

        return Ok(new { userId = id, following });
    }
}
