using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Talos.Server.Controllers;
using Talos.Server.Data;
using Talos.Server.Models;
using Talos.Server.Services;
using Xunit;

namespace Talos.Server.Tests.Controllers;

public class PackageManagersControllerTests
{
    private readonly AppDbContext _context;
    private readonly PackageManagersController _controller;

    public PackageManagersControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        var service = new PackageManagerService(_context);
        var loggerMock = new Mock<ILogger<PackageManagersController>>();

        _controller = new PackageManagersController(_context, loggerMock.Object, service);

        SeedData();
    }

    private void SeedData()
    {
        _context.PackageManagers.AddRange(
            new PackageManager { Id = 1, Name = "npm" },
            new PackageManager { Id = 2, Name = "pip" }
        );
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetAllManagers_ReturnsOk()
    {
        var result = await _controller.GetAllManagers();

        var ok = Assert.IsType<OkObjectResult>(result);
        
        // Using System.Text.Json to verify content
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        var root = System.Text.Json.JsonDocument.Parse(json).RootElement;
        
        Assert.Equal(2, root.GetArrayLength());
    }

    [Fact]
    public async Task GetManagerById_ReturnsManager()
    {
        var result = await _controller.GetManagerById(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        var root = System.Text.Json.JsonDocument.Parse(json).RootElement;

        Assert.Equal("npm", root.GetProperty("Name").GetString());
    }

    [Fact]
    public async Task GetManagerById_ReturnsNotFound()
    {
        var result = await _controller.GetManagerById(999);
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CreateManager_ReturnsCreated()
    {
        var dto = new PackageManagerDto { Name = "cargo" };
        var result = await _controller.CreateManager(dto);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var manager = Assert.IsType<PackageManager>(created.Value);
        Assert.Equal("cargo", manager.Name);
    }

    [Fact]
    public async Task CreateManager_ReturnsConflict()
    {
        var dto = new PackageManagerDto { Name = "npm" };
        var result = await _controller.CreateManager(dto);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task UpdateManager_ReturnsOk()
    {
        var dto = new PackageManagerDto { Name = "npm-updated" };
        var result = await _controller.UpdateManager(1, dto);

        var ok = Assert.IsType<OkObjectResult>(result);
        var manager = Assert.IsType<PackageManager>(ok.Value);
        Assert.Equal("npm-updated", manager.Name);
    }

    [Fact]
    public async Task UpdateManager_ReturnsConflict()
    {
        var dto = new PackageManagerDto { Name = "pip" }; // Exists with ID 2
        var result = await _controller.UpdateManager(1, dto); // Updating ID 1

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task DeleteManager_ReturnsNoContent()
    {
        var result = await _controller.DeleteManager(1);
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteManager_ReturnsNotFound()
    {
        var result = await _controller.DeleteManager(999);
        Assert.IsType<NotFoundObjectResult>(result);
    }
}
