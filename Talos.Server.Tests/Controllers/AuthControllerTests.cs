using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Talos.Server.Data;
using Talos.Server.Models;
using Talos.Server.Models.DTOs.Auth;
using Talos.Server.Models.DTOs.Users;
using Talos.Server.Services.Auth;
using Xunit;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly AppDbContext _context;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);

        _controller = new AuthController(
            _authServiceMock.Object,
            _context
        );
    }

    // ---------------- LOGIN ----------------

    [Fact]
    public async Task Login_ReturnsOk_WhenCredentialsAreValid()
    {
        var dto = new LoginDto
        {
            Email = "test@mail.com",
            Password = "123456"
        };

        _authServiceMock
            .Setup(s => s.LoginAsync(dto.Email, dto.Password))
            .ReturnsAsync(new AuthResponseDto
            {
                Success = true,
                Token = "jwt"
            });

        var result = await _controller.Login(dto);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponseDto>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenInvalid()
    {
        var dto = new LoginDto
        {
            Email = "test@mail.com",
            Password = "wrong"
        };

        _authServiceMock
            .Setup(s => s.LoginAsync(dto.Email, dto.Password))
            .ReturnsAsync(new AuthResponseDto
            {
                Success = false,
                Error = "Invalid credentials"
            });

        var result = await _controller.Login(dto);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    // ---------------- REGISTER ----------------

    [Fact]
    public async Task Register_ReturnsOk_WhenSuccess()
    {
        var dto = new UserRegisterDto
        {
            Email = "new@mail.com",
            Username = "newuser",
            Password = "123456",
            ConfirmPassword = "123456"
        };

        _authServiceMock
            .Setup(s => s.RegisterAsync(dto))
            .ReturnsAsync(new AuthResponseDto { Success = true });

        var result = await _controller.Register(dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenFails()
    {
        var dto = new UserRegisterDto
        {
            Email = "new@mail.com",
            Username = "newuser",
            Password = "123456",
            ConfirmPassword = "123456"
        };

        _authServiceMock
            .Setup(s => s.RegisterAsync(dto))
            .ReturnsAsync(new AuthResponseDto
            {
                Success = false,
                Error = "Email already exists"
            });

        Assert.IsType<BadRequestObjectResult>(
            await _controller.Register(dto)
        );
    }

    // ---------------- REFRESH ----------------

    [Fact]
    public async Task Refresh_ReturnsBadRequest_WhenEmptyToken()
    {
        var result = await _controller.Refresh(new TokenDto
        {
            RefreshToken = ""
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Refresh_ReturnsOk_WhenValidToken()
    {
        _authServiceMock
            .Setup(s => s.RefreshTokenAsync("valid"))
            .ReturnsAsync(new AuthResponseDto
            {
                Success = true,
                Token = "new-jwt"
            });

        var result = await _controller.Refresh(new TokenDto
        {
            RefreshToken = "valid"
        });

        Assert.IsType<OkObjectResult>(result);
    }

    // ---------------- PROFILE ----------------

    [Fact]
    public async Task GetProfile_ReturnsUnauthorized_WhenNoClaims()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        Assert.IsType<UnauthorizedObjectResult>(
            await _controller.GetProfile()
        );
    }

    [Fact]
    public async Task GetProfile_ReturnsOk_WhenUserExists()
    {
        var user = new User
        {
            Id = 1,
            Username = "test",
            Email = "test@mail.com",
            PasswordHash = "hash",
            Role = "user",
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var claims = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, "1") },
                "TestAuth"
            )
        );

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claims }
        };

        var result = await _controller.GetProfile();

        var ok = Assert.IsType<OkObjectResult>(result);
        var profile = Assert.IsType<UserProfileDto>(ok.Value);

        Assert.Equal(user.Email, profile.Email);
    }
    [Fact]
    public async Task GetProfile_ReturnsNotFound_WhenUserDoesNotExist()
    {
        var claims = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, "999") },
                "TestAuth"
            )
        );

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claims }
        };

        var result = await _controller.GetProfile();
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ---------------- LOGOUT ----------------

    [Fact]
    public async Task Logout_ReturnsOk_WhenSuccess()
    {
        _authServiceMock
            .Setup(s => s.RevokeRefreshTokenAsync("valid"))
            .ReturnsAsync(true);

        var result = await _controller.Logout(new TokenDto { RefreshToken = "valid" });
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Logout_ReturnsNotFound_WhenFails()
    {
        _authServiceMock
            .Setup(s => s.RevokeRefreshTokenAsync("invalid"))
            .ReturnsAsync(false);

        var result = await _controller.Logout(new TokenDto { RefreshToken = "invalid" });
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Logout_ReturnsBadRequest_WhenEmptyToken()
    {
        var result = await _controller.Logout(new TokenDto { RefreshToken = "" });
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ---------------- UPDATE PROFILE ----------------

    [Fact]
    public async Task UpdateProfile_ReturnsOk_WhenSuccess()
    {
        var user = new User
        {
            Id = 2,
            Username = "user2",
            Email = "user2@mail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            Role = "user",
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var claims = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, "2") },
                "TestAuth"
            )
        );
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claims }
        };

        var dto = new UpdateProfileDto
        {
            Username = "user2_updated",
            Email = "user2_updated@mail.com"
        };

        var result = await _controller.UpdateProfile(dto);
        Assert.IsType<OkObjectResult>(result);

        var updatedUser = await _context.Users.FindAsync(2);
        Assert.Equal("user2_updated", updatedUser.Username);
        Assert.Equal("user2_updated@mail.com", updatedUser.Email);
    }

    [Fact]
    public async Task UpdateProfile_ReturnsConflict_WhenUsernameExists()
    {
        _context.Users.AddRange(
            new User { Id = 3, Username = "user3", Email = "user3@mail.com", PasswordHash = "hash" },
            new User { Id = 4, Username = "existing", Email = "existing@mail.com", PasswordHash = "hash" }
        );
        await _context.SaveChangesAsync();

        var claims = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, "3") },
                "TestAuth"
            )
        );
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claims }
        };

        var dto = new UpdateProfileDto { Username = "existing" };
        var result = await _controller.UpdateProfile(dto);
        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task UpdateProfile_ReturnsBadRequest_WhenPasswordIncorrect()
    {
        var user = new User
        {
            Id = 5,
            Username = "user5",
            Email = "user5@mail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456")
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var claims = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, "5") },
                "TestAuth"
            )
        );
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claims }
        };

        var dto = new UpdateProfileDto
        {
            CurrentPassword = "wrong",
            NewPassword = "new"
        };

        var result = await _controller.UpdateProfile(dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
