using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;//TODO eliminar. No se esta usando pero no lo quiero eliminar por el primer proverbio del ingeniero
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
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
            return new AuthResponseDto { Success = false, Error = "Usuario no encontrado" };

        bool isValid = !string.IsNullOrEmpty(user.PasswordHash) && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

        if (!isValid)
            return new AuthResponseDto { Success = false, Error = "Contraseña incorrecta" };

        var tokenResult = await GenerateJwtTokenAsync(user);

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

        var tokenResult = await GenerateJwtTokenAsync(newUser);

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
        var token = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (token == null) return false;

        _context.RefreshTokens.Remove(token);
        await _context.SaveChangesAsync();

        return true;
    }

    //TODO no medio el tiempo para testearlo bien pero por lo que teste no explota
    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var storedToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (storedToken == null)
                return new AuthResponseDto { Success = false, Error = "Refresh token inválido" };

            if (storedToken.ExpiresAt < DateTime.UtcNow)
                return new AuthResponseDto { Success = false, Error = "Refresh token expirado" };

            if (storedToken.IsRevoked)
                return new AuthResponseDto { Success = false, Error = "Refresh token revocado" };

            // Revocar el token actual
            _context.RefreshTokens.Remove(storedToken);
            await _context.SaveChangesAsync();

            // Generar nuevos tokens
            var tokenResult = await GenerateJwtTokenAsync(storedToken.User);

            return new AuthResponseDto
            {
                Success = true,
                Token = tokenResult.Token,
                RefreshToken = tokenResult.RefreshToken,
                ExpiresAt = tokenResult.ExpiresAt,
                User = new UserDto
                {
                    Id = storedToken.User.Id,
                    Username = storedToken.User.Username,
                    Email = storedToken.User.Email,
                    Role = storedToken.User.Role,
                    CreatedAt = storedToken.User.CreatedAt
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al refrescar token");
            return new AuthResponseDto { Success = false, Error = "Error interno del servidor" };
        }
    }

    private async Task<TokenResult> GenerateJwtTokenAsync(User user)
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

        // Crear refresh token
        var refreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7), // Refresh token válido por 7 días
            UserId = user.Id,
            IsRevoked = false
        };

        // Limpiar tokens antiguos (opcional, mantener solo los últimos N tokens)
        var oldTokens = user.RefreshTokens?.Where(rt => rt.ExpiresAt < DateTime.UtcNow).ToList();
        if (oldTokens?.Any() == true)
        {
            _context.RefreshTokens.RemoveRange(oldTokens);
        }

        // Guardar el nuevo refresh token
        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        return new TokenResult
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            RefreshToken = refreshToken.Token,
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