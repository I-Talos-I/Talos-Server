using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Talos.Server.Data;
using Talos.Server.Models;
using Talos.Server.Models.Dtos;

namespace Talos.Server.Controllers;

[ApiController]
[Route("api/templates")]
public class TemplateController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly IMapper _mapper;

    public TemplateController(AppDbContext context, IDistributedCache cache, IMapper mapper)
    {
        _context = context;
        _cache = cache;
        _mapper = mapper;
    }

    // GET: api/templates
    [HttpGet]
    public async Task<IActionResult> GetAllTemplates(
        [FromQuery] bool? isPublic = null,
        [FromQuery] string? licenseType = null,
        [FromQuery] int? userId = null)
    {
        try
        {
            string cacheKey = $"templates_all_{isPublic}_{licenseType}_{userId}";
            var cached = await _cache.GetStringAsync(cacheKey);

            if (cached != null)
            {
                var result = JsonSerializer.Deserialize<List<TemplateDto>>(cached);
                return Ok(new { source = "redis-cache", data = result });
            }

            var query = _context.Templates.AsNoTracking().AsQueryable();

            if (isPublic.HasValue)
                query = query.Where(t => t.IsPublic == isPublic.Value);

            if (!string.IsNullOrWhiteSpace(licenseType))
                query = query.Where(t => t.LicenseType == licenseType);

            if (userId.HasValue)
                query = query.Where(t => t.UserId == userId);

            var templates = await query.ToListAsync();
            var dto = _mapper.Map<List<TemplateDto>>(templates);

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });

            return Ok(new { source = "database", data = dto });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", detail = ex.Message });
        }
    }

    // GET: api/templates/user/{userId}
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetTemplatesByUser(int userId)
    {
        try
        {
            string cacheKey = $"templates_user_{userId}";
            var cached = await _cache.GetStringAsync(cacheKey);

            if (cached != null)
            {
                var result = JsonSerializer.Deserialize<List<TemplateDto>>(cached);
                return Ok(new { source = "redis-cache", data = result });
            }

            var templates = await _context.Templates
                .AsNoTracking()
                .Where(t => t.UserId == userId)
                .ToListAsync();

            var dto = _mapper.Map<List<TemplateDto>>(templates);

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });

            return Ok(new { source = "database", data = dto });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", detail = ex.Message });
        }
    }

    // GET: api/templates/search
    [HttpGet("search")]
    public async Task<IActionResult> SearchTemplates([FromQuery] string q)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { message = "Search query is required" });

            string cacheKey = $"templates_search_{q.ToLower()}";
            var cached = await _cache.GetStringAsync(cacheKey);

            if (cached != null)
            {
                var result = JsonSerializer.Deserialize<List<TemplateDto>>(cached);
                return Ok(new { source = "redis-cache", data = result });
            }

            var templates = await _context.Templates
                .AsNoTracking()
                .Where(t =>
                    t.TemplateName.ToLower().Contains(q.ToLower()) ||
                    t.Slug.Contains(q.ToLower().Replace(" ", "-")))
                .ToListAsync();

            var dto = _mapper.Map<List<TemplateDto>>(templates);

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });

            return Ok(new { source = "database", data = dto });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", detail = ex.Message });
        }
    }

    // GET: api/templates/featured
    [HttpGet("featured")]
    public async Task<IActionResult> GetFeaturedTemplates()
    {
        try
        {
            string cacheKey = "templates_featured";
            var cached = await _cache.GetStringAsync(cacheKey);

            if (cached != null)
            {
                var result = JsonSerializer.Deserialize<List<TemplateDto>>(cached);
                return Ok(new { source = "redis-cache", data = result });
            }

            var templates = await _context.Templates
                .AsNoTracking()
                .Where(t => t.IsPublic)
                .OrderByDescending(t => t.CreateAt)
                .Take(10)
                .ToListAsync();

            var dto = _mapper.Map<List<TemplateDto>>(templates);

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) });

            return Ok(new { source = "database", data = dto });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", detail = ex.Message });
        }
    }

    // GET: api/templates/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTemplateById(int id)
    {
        try
        {
            string cacheKey = $"template_{id}";
            var cached = await _cache.GetStringAsync(cacheKey);

            if (cached != null)
            {
                var result = JsonSerializer.Deserialize<TemplateDto>(cached);
                return Ok(new { source = "redis-cache", data = result });
            }

            var template = await _context.Templates
                .AsNoTracking()
                .Include(t => t.TemplateDependencies)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template == null)
                return NotFound(new { message = "Template not found" });

            var dto = _mapper.Map<TemplateDto>(template);

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });

            return Ok(new { source = "database", data = dto });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", detail = ex.Message });
        }
    }


    // POST: api/templates
    [HttpPost]
    
    public async Task<IActionResult> CreateTemplate([FromBody] TemplateCreateDto dto)

    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.Users.OrderBy(u => u.Id).FirstOrDefaultAsync();
            if (user == null)
                return StatusCode(500, new { message = "No users available" });

            // Validation: template must be unique to the user
            var exists = await _context.Templates
                .AnyAsync(t => t.UserId == user.Id && t.TemplateName.ToLower() == dto.Template_Name.ToLower());

            if (exists)
                return Conflict(new { message = "A template with this name already exists" });

            var entity = _mapper.Map<Template>(dto);

            entity.UserId = user.Id;
            entity.Slug = dto.Template_Name.ToLower().Replace(" ", "-");
            entity.CreateAt = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(entity.LicenseType))
                entity.LicenseType = "MIT";

            await _context.Templates.AddAsync(entity);
            await _context.SaveChangesAsync();

            await ClearRelatedCaches(user.Id);

            return CreatedAtAction(nameof(GetTemplateById), new { id = entity.Id }, _mapper.Map<TemplateDto>(entity));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal error", detail = ex.Message });
        }
    }


    // PUT: api/templates/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTemplate(int id, [FromBody] TemplateCreateDto dto)
    {
        var template = await _context.Templates.FirstOrDefaultAsync(t => t.Id == id);
        if (template == null)
            return NotFound(new { message = "Template not found" });

        template.TemplateName = dto.Template_Name;
        template.Slug = dto.Template_Name.ToLower().Replace(" ", "-");
        template.IsPublic = dto.Is_Public;
        template.LicenseType = string.IsNullOrWhiteSpace(dto.License_Type) ? "MIT" : dto.License_Type;

        await _context.SaveChangesAsync();
        await ClearRelatedCaches(template.UserId, id);

        return Ok(_mapper.Map<TemplateDto>(template));
    }

    // DELETE: api/templates/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTemplate(int id)
    {
        var template = await _context.Templates.FirstOrDefaultAsync(t => t.Id == id);
        if (template == null)
            return NotFound(new { message = "Template not found" });

        _context.Templates.Remove(template);
        await _context.SaveChangesAsync();

        await ClearRelatedCaches(template.UserId, id);
        return NoContent();
    }

    private async Task ClearRelatedCaches(int userId, int? templateId = null)
    {
        var tasks = new List<Task>
        {
            _cache.RemoveAsync("templates_featured"),
            _cache.RemoveAsync($"templates_user_{userId}")
        };

        if (templateId.HasValue)
            tasks.Add(_cache.RemoveAsync($"template_{templateId.Value}"));

        await Task.WhenAll(tasks);
    }
}
