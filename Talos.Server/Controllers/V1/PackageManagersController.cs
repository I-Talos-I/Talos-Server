using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Talos.Server.Data;
using Talos.Server.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;

namespace Talos.Server.Controllers;
[Authorize(Roles = "admin")]
[ApiController]
[Route("api/v1/package-managers")]
public class PackageManagersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<PackageManagersController> _logger;

    public PackageManagersController(
        AppDbContext context,
        ILogger<PackageManagersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/package-managers
    [HttpGet]
    public async Task<IActionResult> GetAllManagers()
    {
        try
        {
            var managers = await _context.PackageManagers
                .Select(m => new
                {
                    m.Id,
                    m.Name,
                    packagesCount = m.Packages.Count
                })
                .OrderBy(m => m.Name)
                .ToListAsync();

            return Ok(managers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo gestores de paquetes");
            return StatusCode(500, new { message = "Error interno", detail = ex.Message });
        }
    }

    // GET: api/package-managers/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetManagerById(int id)
    {
        try
        {
            var manager = await _context.PackageManagers
                .Include(m => m.Packages)
                .Where(m => m.Id == id)
                .Select(m => new
                {
                    m.Id,
                    m.Name,
                    packages = m.Packages.Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.ShortName,
                        p.IsActive
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (manager == null)
                return NotFound(new { message = "Gestor de paquetes no encontrado" });

            return Ok(manager);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error obteniendo gestor {id}");
            return StatusCode(500, new { message = "Error interno", detail = ex.Message });
        }
    }

    // POST: api/package-managers
    [HttpPost]
    public async Task<IActionResult> CreateManager([FromBody] PackageManagerDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validar duplicados
            var exists = await _context.PackageManagers.AnyAsync(m => m.Name == dto.Name);
            if (exists)
                return Conflict(new { message = "Ya existe un gestor con ese nombre" });

            var manager = new PackageManager
            {
                Name = dto.Name
            };

            _context.PackageManagers.Add(manager);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetManagerById), new { id = manager.Id }, manager);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando gestor de paquetes");
            return StatusCode(500, new { message = "Error interno", detail = ex.Message });
        }
    }

    // PUT: api/package-managers/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateManager(int id, [FromBody] PackageManagerDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var manager = await _context.PackageManagers.FindAsync(id);
            if (manager == null)
                return NotFound(new { message = "Gestor de paquetes no encontrado" });

            // Validar nombre duplicado
            var exists = await _context.PackageManagers
                .AnyAsync(m => m.Name == dto.Name && m.Id != id);
            if (exists)
                return Conflict(new { message = "Otro gestor ya tiene ese nombre" });

            manager.Name = dto.Name;
            await _context.SaveChangesAsync();

            return Ok(manager);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error actualizando gestor {id}");
            return StatusCode(500, new { message = "Error interno", detail = ex.Message });
        }
    }

    // DELETE: api/package-managers/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteManager(int id)
    {
        try
        {
            var manager = await _context.PackageManagers
                .Include(m => m.Packages)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (manager == null)
                return NotFound(new { message = "Gestor de paquetes no encontrado" });

            if (manager.Packages.Any())
                return BadRequest(new { message = "No se puede eliminar un gestor que tiene paquetes asociados" });

            _context.PackageManagers.Remove(manager);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error eliminando gestor {id}");
            return StatusCode(500, new { message = "Error interno", detail = ex.Message });
        }
    }
}

// DTO para crear/editar
public class PackageManagerDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; }
}
