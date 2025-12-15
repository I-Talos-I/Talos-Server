using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Talos.Server.Controllers;
using Talos.Server.Data;
using Talos.Server.Models;
using Xunit;
using AutoMapper;

public class PackagesControllerTests
{
    private readonly AppDbContext _context;
    private readonly PackagesController _controller;

    public PackagesControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);

        SeedData();

        var mapperMock = new Mock<IMapper>();
        var loggerMock = new Mock<ILogger<PackagesController>>();

        _controller = new PackagesController(
            _context,
            mapperMock.Object,
            loggerMock.Object
        );

        SetAdminUser();
    }

    // ------------------ HELPERS ------------------

    private void SetAdminUser()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "admin")
        };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(claims, "TestAuth"))
            }
        };
    }

    private void SeedData()
    {
        var manager = new PackageManager
        {
            Id = 1,
            Name = "npm"
        };

        var package = new Package
        {
            Id = 1,
            Name = "React",
            ShortName = "react",
            RepositoryUrl = "https://github.com/facebook/react",
            OfficialDocumentationUrl = "https://react.dev",
            IsActive = true,
            CreateAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow,
            PackageManagerId = 1,
            PackageManager = manager
        };

        var version = new PackageVersion
        {
            Id = 1,
            PackageId = 1,
            Version = "18.2.0",
            ReleaseDate = DateTime.UtcNow.AddDays(-10),
            IsDeprecated = false,
            DeprecationMessage = "",
            DownloadUrl = "https://registry.npmjs.org/react/-/react-18.2.0.tgz",
            ReleaseNotesUrl = "https://github.com/facebook/react/releases/tag/v18.2.0"
        };

        _context.PackageManagers.Add(manager);
        _context.Packages.Add(package);
        _context.PackageVersions.Add(version);

        _context.SaveChanges();
    }

    // ------------------ TESTS ------------------

    [Fact]
    public async Task GetAllPackages_ReturnsOk()
    {
        var result = await _controller.GetAllPackages();

        var ok = Assert.IsType<OkObjectResult>(result);
        var data = ok.Value as dynamic;

        Assert.True(data.totalItems > 0);
    }

    [Fact]
    public async Task GetPackageById_ReturnsPackage()
    {
        var result = await _controller.GetPackageById(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        var data = ok.Value as dynamic;

        Assert.Equal("React", (string)data.Name);
    }

    [Fact]
    public async Task GetPackageById_ReturnsNotFound()
    {
        var result = await _controller.GetPackageById(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetAllByManager_ReturnsPackages()
    {
        var result = await _controller.GetAllByManager(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        var data = ok.Value as dynamic;

        Assert.Equal("npm", (string)data.manager.Name);
        Assert.True(data.total > 0);
    }

    [Fact]
    public async Task GetAllByManager_ReturnsNotFound()
    {
        var result = await _controller.GetAllByManager(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeletePackage_ReturnsOk_WhenNoDependencies()
    {
        var result = await _controller.DeletePackage(1);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task DeletePackage_ReturnsNotFound()
    {
        var result = await _controller.DeletePackage(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
