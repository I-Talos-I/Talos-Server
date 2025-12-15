using Microsoft.EntityFrameworkCore;
using Talos.Server.Data;
using Talos.Server.Models;
using Talos.Server.Services;
using Xunit;

namespace Talos.Server.Tests.Services;

public class PackageManagerServiceTests
{
    private readonly AppDbContext _context;
    private readonly PackageManagerService _service;

    public PackageManagerServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _service = new PackageManagerService(_context);

        SeedData();
    }

    private void SeedData()
    {
        _context.PackageManagers.Add(new PackageManager { Id = 1, Name = "npm" });
        _context.PackageManagers.Add(new PackageManager { Id = 2, Name = "pip" });
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetByNameAsync_ReturnsManager_WhenExists()
    {
        var result = await _service.GetByNameAsync("npm");

        Assert.NotNull(result);
        Assert.Equal("npm", result.Name);
    }

    [Fact]
    public async Task GetByNameAsync_ReturnsNull_WhenNotExists()
    {
        var result = await _service.GetByNameAsync("cargo");

        Assert.Null(result);
    }
}
