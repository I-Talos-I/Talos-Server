using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Talos.Server.Data;
using Talos.Server.Models;
using Talos.Server.Models.Dtos.Auth;
using Talos.Server.Models.DTOs.Auth;
using Talos.Server.Models.DTOs.Users;
using Talos.Server.Models.Entities;
using Talos.Server.Services.Auth;
using Talos.Server.Services.Interfaces;
using RefreshTokenEntity = Talos.Server.Models.Entities.RefreshToken;


namespace Talos.Server.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    private readonly IUserStatusService _userStatusService;

    public AuthService(AppDbContext context, IConfiguration configuration, ILogger<AuthService> logger,IUserStatusService userStatusService)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _userStatusService = userStatusService;
    }

    public async Task<AuthResponseDto> LoginAsync(string email, string password)
    {
        try
        {
            _logger.LogInformation($"Login attempt for: {email}");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Error = "Usuario no encontrado"
                };
            }

            // Verificar contraseña
            bool isPasswordValid = false;
            
            if (!string.IsNullOrEmpty(user.PasswordHash))
            {
                isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            }

            if (!isPasswordValid)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Error = "Contraseña incorrecta"
                };
            }

            // Generar token
            var tokenResult = GenerateJwtToken(user);
            var refreshToken = await CreateRefreshTokenAsync(user);
           // await _userStatusService.SetUserOnlineAsync(user.Id);
            
            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.UserName,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };

            return new AuthResponseDto
            {
                Success = true,
                Token = tokenResult.Token,
                RefreshToken = refreshToken.Token,
                ExpiresAt = tokenResult.ExpiresAt,
                User = userDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error");
            return new AuthResponseDto
            {
                Success = false,
                Error = "Error interno"
            };
        }
    }

   public async Task<AuthResponseDto> RegisterAsync(UserRegisterDto registerDto)
{
    try
    {
        _logger.LogInformation($"Register attempt for email: {registerDto.Email}, username: {registerDto.Username}");

        // Validaciones adicionales
        if (string.IsNullOrWhiteSpace(registerDto.Email))
        {
            _logger.LogWarning("Email is empty");
            return new AuthResponseDto { Success = false, Error = "Email es requerido" };
        }

        if (string.IsNullOrWhiteSpace(registerDto.Password))
        {
            _logger.LogWarning("Password is empty");
            return new AuthResponseDto { Success = false, Error = "Contraseña es requerida" };
        }

        if (registerDto.Password != registerDto.ConfirmPassword)
        {
            _logger.LogWarning("Passwords don't match");
            return new AuthResponseDto { Success = false, Error = "Las contraseñas no coinciden" };
        }

        // Verificar si el email ya existe
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == registerDto.Email);

        if (existingUser != null)
        {
            _logger.LogWarning($"Email already exists: {registerDto.Email}");
            return new AuthResponseDto
            {
                Success = false,
                Error = "El email ya está registrado"
            };
        }

        // Verificar si el username ya existe
        var existingUsername = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == registerDto.Username);

        if (existingUsername != null)
        {
            _logger.LogWarning($"Username already exists: {registerDto.Username}");
            return new AuthResponseDto
            {
                Success = false,
                Error = "El nombre de usuario ya existe"
            };
        }

        _logger.LogInformation("Creating new user...");

        // Crear nuevo usuario
        var newUser = new User
        {
            UserName = registerDto.Username,
            Email = registerDto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
            Role = "user",
            CreatedAt = DateTime.UtcNow
        };

        _logger.LogInformation($"User object created: {newUser.UserName}, {newUser.Email}");

        await _context.Users.AddAsync(newUser);
        
        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation($"User saved successfully with ID: {newUser.Id}");
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Database error saving user");
            _logger.LogError($"Inner exception: {dbEx.InnerException?.Message}");
            return new AuthResponseDto
            {
                Success = false,
                Error = $"Error de base de datos: {dbEx.InnerException?.Message}"
            };
        }

        // Generar token
        var tokenResult = GenerateJwtToken(newUser);
        var refreshToken = await CreateRefreshTokenAsync(newUser);
        
        var userDto = new UserDto
        {
            Id = newUser.Id,
            Username = newUser.UserName,
            Email = newUser.Email,
            Role = newUser.Role,
            CreatedAt = newUser.CreatedAt
        };

        _logger.LogInformation($"Registration successful for user: {newUser.UserName}");

        return new AuthResponseDto
        {
            Success = true,
            Token = tokenResult.Token,
            RefreshToken = refreshToken.Token,
            ExpiresAt = tokenResult.ExpiresAt,
            User = userDto
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Registration error for email: {registerDto.Email}");
        return new AuthResponseDto
        {
            Success = false,
            Error = $"Error interno: {ex.Message}"
        };
    }
}

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (storedToken == null)
        {
            return new AuthResponseDto
            {
                Success = false,
                Error = "Refresh token inválido"
            };
        }

        if (storedToken.IsRevoked)
        {
            return new AuthResponseDto
            {
                Success = false,
                Error = "Refresh token revocado"
            };
        }

        if (storedToken.ExpiresAt < DateTime.UtcNow)
        {
            return new AuthResponseDto
            {
                Success = false,
                Error = "Refresh token expirado"
            };
        }

        // Revocar el refresh token actual
        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;

        // Generar nuevos tokens
        var tokenResult = GenerateJwtToken(storedToken.User);
        var newRefreshToken = await CreateRefreshTokenAsync(storedToken.User);

        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            Success = true,
            Token = tokenResult.Token,
            RefreshToken = newRefreshToken.Token,
            ExpiresAt = tokenResult.ExpiresAt,
            User = new UserDto
            {
                Id = storedToken.User.Id,
                Username = storedToken.User.UserName,
                Email = storedToken.User.Email,
                Role = storedToken.User.Role,
                CreatedAt = storedToken.User.CreatedAt
            }
        };
    }


    private TokenResult GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var expires = DateTime.UtcNow.AddMinutes(60);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new TokenResult
        {
            Token = tokenString,
            ExpiresAt = expires
        };
    }
    
    private async Task<RefreshTokenEntity> CreateRefreshTokenAsync(User user)
    {
        var refreshToken = new RefreshTokenEntity
        {
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return refreshToken;
    }


    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
    {
        try
        {
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (storedToken == null || storedToken.IsRevoked)
                return false;

            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error revocando refresh token: {refreshToken}");
            return false;
        }
    }


    private class TokenResult
    {
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}