using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Talos.Server.Data;
using Talos.Server.Models.Dtos;

namespace Talos.Server.Controllers;

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
        [FromQuery] int page = 1
    ){
        const int pageSize = 10;

        if (page < 1) page = 1;

        try
        {
            var query = _context.Templates
                .AsNoTracking()
                .Where(t => t.IsPublic == false);

            var total = await query.CountAsync();

            var templates = await query
                .OrderBy(t => t.Id) // obligatorio para paginación consistente
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dto = _mapper.Map<List<TemplateDto>>(templates);

            return Ok(new
            {
                data = dto,
                pagination = new
                {
                    page,
                    pageSize,
                    total,
                    totalPages = (int)Math.Ceiling(total / (double)pageSize)
                }
            });
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

        var search = q.Trim();
        var slugSearch = search.Replace(" ", "-");

        var templates = await _context.Templates
            .AsNoTracking()
            .Where(t =>
                EF.Functions.Like(t.Name, $"%{search}%") ||
                EF.Functions.Like(t.Slug, $"%{slugSearch}%")
            )
            .OrderBy(t => t.Name)
            .Take(50)
            .Select(t => new TemplateDto
            {
                Id = t.Id,
                Name = t.Name,
                Slug = t.Slug,
                // agrega solo lo que realmente necesitas
            })
            .ToListAsync();

        return Ok(templates);
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
                .OrderByDescending(t => t.CreatedAt)
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
                .Include(t => t.Dependencies)
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
    
    // GET: api/templates/{slug}
    [HttpGet("by-slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return BadRequest(new { message = "Slug is required" });

        var template = await _context.Templates
            .AsNoTracking()
            .Include(t => t.Dependencies)
            .FirstOrDefaultAsync(t => t.Slug == slug);

        if (template == null)
            return NotFound(new { message = "Template not found" });

        return Ok(_mapper.Map<TemplateDto>(template));
    }

    // POST: api/templates
    [HttpPost]
    [Authorize(Roles = "admin,user")]
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
                t.Name.ToLower() == dto.Name.ToLower());

            if (exists)
                return Conflict(new { message = "A template with this name already exists" });

            var entity = _mapper.Map<Template>(dto);
            entity.UserId = user.Id;
            entity.Slug = dto.Name.ToLower().Replace(" ", "-");
            entity.CreatedAt = DateTime.UtcNow;
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
    [Authorize(Roles = "admin,user")]
    public async Task<IActionResult> UpdateTemplate(int id, [FromBody] TemplateCreateDto dto)
    {
        var template = await _context.Templates.FirstOrDefaultAsync(t => t.Id == id);
        if (template == null)
            return NotFound(new { message = "Template not found" });

        template.Name = dto.Name;
        template.Slug = dto.Name.ToLower().Replace(" ", "-");
        template.IsPublic = dto.IsPublic;
        template.LicenseType = string.IsNullOrWhiteSpace(dto.LicenseType)
            ? "MIT"
            : dto.LicenseType;

        await _context.SaveChangesAsync();

        return Ok(_mapper.Map<TemplateDto>(template));
    }

    // DELETE: api/templates/{id}
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin,user")]
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
