using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Talos.Server.Data;
using Talos.Server.Models;
using Talos.Server.Models.Entities;
using Talos.Server.Models.DTOs.Auth;
using Talos.Server.Models.DTOs.Users;

namespace Talos.Server.Services.Auth;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResponseDto> LoginAsync(string email, string password)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
            return new AuthResponseDto { Success = false, Error = "Usuario no encontrado" };

        bool isValid = !string.IsNullOrEmpty(user.PasswordHash) && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

        if (!isValid)
            return new AuthResponseDto { Success = false, Error = "Contraseña incorrecta" };

        var tokenResult = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Success = true,
            Token = tokenResult.Token,
            RefreshToken = tokenResult.RefreshToken,
            ExpiresAt = tokenResult.ExpiresAt,
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            }
        };
    }

    public async Task<AuthResponseDto> RegisterAsync(UserRegisterDto registerDto)
    {
        if (string.IsNullOrEmpty(registerDto.Email))
            return new AuthResponseDto { Success = false, Error = "Email es requerido" };

        if (registerDto.Password != registerDto.ConfirmPassword)
            return new AuthResponseDto { Success = false, Error = "Las contraseñas no coinciden" };

        if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
            return new AuthResponseDto { Success = false, Error = "El email ya está registrado" };

        if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username))
            return new AuthResponseDto { Success = false, Error = "El nombre de usuario ya existe" };

        var newUser = new User
        {
            Username = registerDto.Username,
            Email = registerDto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
            Role = "user",
            CreatedAt = DateTime.UtcNow
        };

        await _context.Users.AddAsync(newUser);
        await _context.SaveChangesAsync();

        var tokenResult = GenerateJwtToken(newUser);

        return new AuthResponseDto
        {
            Success = true,
            Token = tokenResult.Token,
            RefreshToken = tokenResult.RefreshToken,
            ExpiresAt = tokenResult.ExpiresAt,
            User = new UserDto
            {
                Id = newUser.Id,
                Username = newUser.Username,
                Email = newUser.Email,
                Role = newUser.Role,
                CreatedAt = newUser.CreatedAt
            }
        };
    }

    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
    {
        var user = await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(rt => rt.Token == refreshToken));

        if (user == null) return false;

        var token = user.RefreshTokens.First(rt => rt.Token == refreshToken);

        _context.RefreshTokens.Remove(token);
        await _context.SaveChangesAsync();

        return true;
    }

    public Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        return Task.FromResult(new AuthResponseDto
        {
            Success = false,
            Error = "No implementado"
        });
    }

    private TokenResult GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var expires = DateTime.UtcNow.AddMinutes(60);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new TokenResult
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            RefreshToken = Guid.NewGuid().ToString(),
            ExpiresAt = expires
        };
    }

    private class TokenResult
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
