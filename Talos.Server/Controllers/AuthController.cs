using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Talos.Server.Data;
using Talos.Server.Models.DTOs.Auth;
using Talos.Server.Services.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    private readonly AppDbContext _context;  

    public AuthController(
        IAuthService authService, 
        ILogger<AuthController> logger,
        AppDbContext context)  
    {
        _authService = authService;
        _logger = logger;
        _context = context;  
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.LoginAsync(model.Email, model.Password);
        return result.Success ? Ok(result) : Unauthorized(new { message = result.Error ?? "Invalid credentials" });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegisterDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.RegisterAsync(model);
        return result.Success ? Ok(result) : BadRequest(new { message = result.Error });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] TokenDto dto)
    {
        if (string.IsNullOrEmpty(dto.RefreshToken))
            return BadRequest(new { message = "Refresh token is required" });

        var result = await _authService.RefreshTokenAsync(dto.RefreshToken);
        return result.Success ? Ok(result) : Unauthorized(new { message = result.Error ?? "Invalid refresh token" });
    }
    
    // GET: api/auth/profile
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            // Obtener ID del usuario desde el token JWT
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Token inválido" });
            }

            var user = await _context.Users
                .Include(u => u.Templates)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new { message = "Usuario no encontrado" });
            }

            var profileDto = new UserProfileDto
            {
                Id = user.Id,
                Username = user.UserName,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                TemplateCount = user.Templates?.Count ?? 0
            };

            return Ok(profileDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo perfil");
            return StatusCode(500, new { message = "Error interno" });
        }
    }

    // PUT: api/auth/profile
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Obtener ID del usuario desde el token JWT
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Token inválido" });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "Usuario no encontrado" });
            }

            // Verificar si el nuevo username ya existe (si se cambia)
            if (!string.IsNullOrEmpty(updateDto.Username) && updateDto.Username != user.UserName)
            {
                var existingUsername = await _context.Users
                    .AnyAsync(u => u.UserName == updateDto.Username && u.Id != userId);
                
                if (existingUsername)
                {
                    return Conflict(new { message = "El nombre de usuario ya está en uso" });
                }
                
                user.UserName = updateDto.Username;
            }

            // Verificar si el nuevo email ya existe (si se cambia)
            if (!string.IsNullOrEmpty(updateDto.Email) && updateDto.Email != user.Email)
            {
                var existingEmail = await _context.Users
                    .AnyAsync(u => u.Email == updateDto.Email && u.Id != userId);
                
                if (existingEmail)
                {
                    return Conflict(new { message = "El email ya está en uso" });
                }
                
                user.Email = updateDto.Email;
            }

            // Actualizar contraseña si se proporciona
            if (!string.IsNullOrEmpty(updateDto.CurrentPassword) && 
                !string.IsNullOrEmpty(updateDto.NewPassword))
            {
                // Verificar contraseña actual
                if (!BCrypt.Net.BCrypt.Verify(updateDto.CurrentPassword, user.PasswordHash))
                {
                    return BadRequest(new { message = "Contraseña actual incorrecta" });
                }

                // Actualizar contraseña
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updateDto.NewPassword);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Perfil actualizado correctamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando perfil");
            return StatusCode(500, new { message = "Error interno" });
        }
    }
}