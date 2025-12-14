using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Talos.Server.Data;
using Talos.Server.Models.Dtos.Auth;
using Talos.Server.Models.Entities;

[ApiController]
[Route("api/v1/admin/apikeys")]
public class ApiKeyAdminController : ControllerBase
{
    private readonly AppDbContext _context;

    public ApiKeyAdminController(AppDbContext context) => _context = context;

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateApiKeyDto dto)
    {
        var apiKey = new ApiKey
        {
            Key = ApiKeyGenerator.GenerateKey(),
            Owner = dto.Owner,
            Role = dto.Role,
            Scope = dto.Scope,
            ExpiresAt = dto.ExpiresAt,
            MaxUsage = dto.MaxUsage
        };
        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync();
        return Ok(apiKey);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _context.ApiKeys.ToListAsync());

    [HttpPost("revoke/{id}")]
    public async Task<IActionResult> Revoke(int id)
    {
        var key = await _context.ApiKeys.FindAsync(id);
        if (key == null) return NotFound();
        key.IsActive = false;
        await _context.SaveChangesAsync();
        return Ok(key);
    }
}