using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Talos.Server.Data;
using Talos.Server.Models.DTOs.Auth;
using Talos.Server.Models.DTOs.Users;
using Talos.Server.Services.Auth;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly AppDbContext _context;

    public AuthController(IAuthService authService, AppDbContext context)
    {
        _authService = authService;
        _context = context;
    }

    // LOGIN
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _authService.LoginAsync(model.Email, model.Password);
        return result.Success ? Ok(result) : Unauthorized(new { message = result.Error });
    }

    // REGISTER
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegisterDto model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _authService.RegisterAsync(model);
        return result.Success ? Ok(result) : BadRequest(new { message = result.Error });
    }

    // REFRESH TOKEN
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] TokenDto model)
    {
        if (string.IsNullOrEmpty(model.RefreshToken))
            return BadRequest(new { message = "Refresh token is required" });

        var result = await _authService.RefreshTokenAsync(model.RefreshToken);
        return result.Success ? Ok(result) : Unauthorized(new { message = result.Error });
    }

    // LOGOUT
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] TokenDto model)
    {
        if (string.IsNullOrEmpty(model.RefreshToken))
            return BadRequest(new { message = "Refresh token is required" });

        var revoked = await _authService.RevokeRefreshTokenAsync(model.RefreshToken);
        return revoked ? Ok(new { message = "Logout successful" }) :
                         NotFound(new { message = "Refresh token not found or already revoked" });
    }

    // GET PROFILE
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized(new { message = "Invalid token" });

        var user = await _context.Users
            .Include(u => u.Templates)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return NotFound(new { message = "User not found" });

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

    // UPDATE PROFILE
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto updateDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized(new { message = "Invalid token" });

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound(new { message = "User not found" });

        // Update username
        if (!string.IsNullOrEmpty(updateDto.Username) && updateDto.Username != user.UserName)
        {
            if (await _context.Users.AnyAsync(u => u.UserName == updateDto.Username && u.Id != userId))
                return Conflict(new { message = "Username already in use" });

            user.UserName = updateDto.Username;
        }

        // Update email
        if (!string.IsNullOrEmpty(updateDto.Email) && updateDto.Email != user.Email)
        {
            if (await _context.Users.AnyAsync(u => u.Email == updateDto.Email && u.Id != userId))
                return Conflict(new { message = "Email already in use" });

            user.Email = updateDto.Email;
        }

        // Update password
        if (!string.IsNullOrEmpty(updateDto.CurrentPassword) && !string.IsNullOrEmpty(updateDto.NewPassword))
        {
            if (!BCrypt.Net.BCrypt.Verify(updateDto.CurrentPassword, user.PasswordHash))
                return BadRequest(new { message = "Current password is incorrect" });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updateDto.NewPassword);
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Profile updated successfully" });
    }

    // HELPER: Get user ID from JWT claims
    private int? GetUserIdFromClaims()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null && int.TryParse(claim.Value, out int userId) ? userId : null;
    }
}
