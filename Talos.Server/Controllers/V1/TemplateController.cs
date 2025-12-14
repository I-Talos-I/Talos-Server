using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Talos.Server.Data;
using Talos.Server.Models;
using Talos.Server.Models.Dtos;

namespace Talos.Server.Controllers;
[Authorize(Roles = "admin,user")]

[ApiController]
[Route("api/v1/templates")]
public class TemplateController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public TemplateController(AppDbContext context, IMapper mapper)
    {
        _context = context;
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
            var query = _context.Templates
                .AsNoTracking()
                .AsQueryable();

            if (isPublic.HasValue)
                query = query.Where(t => t.IsPublic == isPublic);

            if (!string.IsNullOrWhiteSpace(licenseType))
                query = query.Where(t => t.LicenseType == licenseType);

            if (userId.HasValue)
                query = query.Where(t => t.UserId == userId);

            var templates = await query.ToListAsync();
            var dto = _mapper.Map<List<TemplateDto>>(templates);

            return Ok(dto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", detail = ex.Message });
        }
    }

    // GET: api/templates/user/{userId}
    [HttpGet("user/{userId:int}")]
    public async Task<IActionResult> GetTemplatesByUser(int userId)
    {
        try
        {
            var templates = await _context.Templates
                .AsNoTracking()
                .Where(t => t.UserId == userId)
                .ToListAsync();

            return Ok(_mapper.Map<List<TemplateDto>>(templates));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", detail = ex.Message });
        }
    }

    // GET: api/templates/search?q=
    [HttpGet("search")]
    public async Task<IActionResult> SearchTemplates([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { message = "Search query is required" });

        try
        {
            var search = q.ToLower();

            var templates = await _context.Templates
                .AsNoTracking()
                .Where(t =>
                    t.TemplateName.ToLower().Contains(search) ||
                    t.Slug.Contains(search.Replace(" ", "-")))
                .ToListAsync();

            return Ok(_mapper.Map<List<TemplateDto>>(templates));
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
            var templates = await _context.Templates
                .AsNoTracking()
                .Where(t => t.IsPublic)
                .OrderByDescending(t => t.CreateAt)
                .Take(10)
                .ToListAsync();

            return Ok(_mapper.Map<List<TemplateDto>>(templates));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", detail = ex.Message });
        }
    }

    // GET: api/templates/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetTemplateById(int id)
    {
        try
        {
            var template = await _context.Templates
                .AsNoTracking()
                .Include(t => t.TemplateDependencies)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template == null)
                return NotFound(new { message = "Template not found" });

            return Ok(_mapper.Map<TemplateDto>(template));
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

            var exists = await _context.Templates.AnyAsync(t =>
                t.UserId == user.Id &&
                t.TemplateName.ToLower() == dto.Template_Name.ToLower());

            if (exists)
                return Conflict(new { message = "A template with this name already exists" });

            var entity = _mapper.Map<Template>(dto);
            entity.UserId = user.Id;
            entity.Slug = dto.Template_Name.ToLower().Replace(" ", "-");
            entity.CreateAt = DateTime.UtcNow;
            entity.LicenseType ??= "MIT";

            _context.Templates.Add(entity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetTemplateById),
                new { id = entity.Id },
                _mapper.Map<TemplateDto>(entity)
            );
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal error", detail = ex.Message });
        }
    }

    // PUT: api/templates/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateTemplate(int id, [FromBody] TemplateCreateDto dto)
    {
        var template = await _context.Templates.FirstOrDefaultAsync(t => t.Id == id);
        if (template == null)
            return NotFound(new { message = "Template not found" });

        template.TemplateName = dto.Template_Name;
        template.Slug = dto.Template_Name.ToLower().Replace(" ", "-");
        template.IsPublic = dto.Is_Public;
        template.LicenseType = string.IsNullOrWhiteSpace(dto.License_Type)
            ? "MIT"
            : dto.License_Type;

        await _context.SaveChangesAsync();

        return Ok(_mapper.Map<TemplateDto>(template));
    }

    // DELETE: api/templates/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTemplate(int id)
    {
        var template = await _context.Templates.FirstOrDefaultAsync(t => t.Id == id);
        if (template == null)
            return NotFound(new { message = "Template not found" });

        _context.Templates.Remove(template);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
