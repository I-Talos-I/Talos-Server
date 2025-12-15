using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Talos.Server.Data;
using Talos.Server.Models;
using Talos.Server.Models.DTOs.Auth;
using Talos.Server.Services;
using Talos.Server.Services.Auth;
using Xunit;

namespace Talos.Server.Tests.Services;

public class AuthServiceTests
{
    // ---------- Helpers ----------
    private static AppDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new AppDbContext(options);
    }

    private static IConfiguration CreateConfiguration()
    {
        var settings = new Dictionary<string, string>
        {
            { "JwtSettings:Key", "THIS_IS_A_SUPER_SECRET_KEY_FOR_TESTING_123456789" },
            { "JwtSettings:Issuer", "TestIssuer" },
            { "JwtSettings:Audience", "TestAudience" }
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings!)
            .Build();
    }

    private static ILogger<AuthService> CreateLogger()
    {
        return Mock.Of<ILogger<AuthService>>();
    }

    private static AuthService CreateService(AppDbContext context)
    {
        return new AuthService(
            context,
            CreateConfiguration(),
            CreateLogger()
        );
    }
    
    // LOGIN TESTS
    

    [Fact]
    public async Task LoginAsync_WhenUserDoesNotExist_ShouldReturnError()
    {
        // Arrange
        var context = CreateDbContext(nameof(LoginAsync_WhenUserDoesNotExist_ShouldReturnError));
        var service = CreateService(context);

        // Act
        var result = await service.LoginAsync("noexiste@mail.com", "123");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Usuario no encontrado");
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIsIncorrect_ShouldReturnError()
    {
        // Arrange
        var context = CreateDbContext(nameof(LoginAsync_WhenPasswordIsIncorrect_ShouldReturnError));

        context.Users.Add(new User
        {
            Username = "testuser",
            Email = "test@mail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword"),
            Role = "user",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.LoginAsync("test@mail.com", "WrongPassword");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Contraseña incorrecta");
    }

    [Fact]
    public async Task LoginAsync_WhenCredentialsAreValid_ShouldReturnTokenAndUser()
    {
        // Arrange
        var context = CreateDbContext(nameof(LoginAsync_WhenCredentialsAreValid_ShouldReturnTokenAndUser));

        var password = "Password123";
        var user = new User
        {
            Username = "validuser",
            Email = "valid@mail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "user",
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.LoginAsync(user.Email, password);

        // Assert
        result.Success.Should().BeTrue();
        result.Token.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be(user.Email);
        result.User.Username.Should().Be(user.Username);
    }

    // =========================================================
    // REGISTER TESTS
    // =========================================================

    [Fact]
    public async Task RegisterAsync_WhenEmailIsEmpty_ShouldReturnError()
    {
        var context = CreateDbContext(nameof(RegisterAsync_WhenEmailIsEmpty_ShouldReturnError));
        var service = CreateService(context);

        var dto = new UserRegisterDto
        {
            Email = "",
            Username = "user",
            Password = "123",
            ConfirmPassword = "123"
        };

        var result = await service.RegisterAsync(dto);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Email es requerido");
    }

    [Fact]
    public async Task RegisterAsync_WhenPasswordsDoNotMatch_ShouldReturnError()
    {
        var context = CreateDbContext(nameof(RegisterAsync_WhenPasswordsDoNotMatch_ShouldReturnError));
        var service = CreateService(context);

        var dto = new UserRegisterDto
        {
            Email = "test@mail.com",
            Username = "user",
            Password = "123",
            ConfirmPassword = "456"
        };

        var result = await service.RegisterAsync(dto);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Las contraseñas no coinciden");
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ShouldReturnError()
    {
        var context = CreateDbContext(nameof(RegisterAsync_WhenEmailAlreadyExists_ShouldReturnError));

        context.Users.Add(new User
        {
            Username = "existing",
            Email = "existing@mail.com",
            PasswordHash = "hash",
            Role = "user",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var dto = new UserRegisterDto
        {
            Email = "existing@mail.com",
            Username = "newuser",
            Password = "123",
            ConfirmPassword = "123"
        };

        var result = await service.RegisterAsync(dto);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("El email ya está registrado");
    }

    [Fact]
    public async Task RegisterAsync_WhenUsernameAlreadyExists_ShouldReturnError()
    {
        var context = CreateDbContext(nameof(RegisterAsync_WhenUsernameAlreadyExists_ShouldReturnError));

        context.Users.Add(new User
        {
            Username = "existinguser",
            Email = "one@mail.com",
            PasswordHash = "hash",
            Role = "user",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var dto = new UserRegisterDto
        {
            Email = "two@mail.com",
            Username = "existinguser",
            Password = "123",
            ConfirmPassword = "123"
        };

        var result = await service.RegisterAsync(dto);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("El nombre de usuario ya existe");
    }

    [Fact]
    public async Task RegisterAsync_WhenDataIsValid_ShouldCreateUserAndReturnToken()
    {
        var context = CreateDbContext(nameof(RegisterAsync_WhenDataIsValid_ShouldCreateUserAndReturnToken));
        var service = CreateService(context);

        var dto = new UserRegisterDto
        {
            Email = "new@mail.com",
            Username = "newuser",
            Password = "Password123",
            ConfirmPassword = "Password123"
        };

        var result = await service.RegisterAsync(dto);

        result.Success.Should().BeTrue();
        result.Token.Should().NotBeNullOrEmpty();
        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be(dto.Email);

        context.Users.Count().Should().Be(1);
    }
}
