using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Talos.Server.Controllers.V1;
using Talos.Server.Data;
using Talos.Server.Models;
using Talos.Server.Models.Entities;
using Xunit;
using OperatingSystem = Talos.Server.Models.Entities.OperatingSystem;

namespace Talos.Server.Tests.Controllers;

public class RegistryControllerTests
{
    private readonly AppDbContext _context;
    private readonly RegistryController _controller;

    public RegistryControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _controller = new RegistryController(_context);

        SeedData();
    }

    private void SeedData()
    {
        var user = new User { Id = 1, Username = "testuser", Email = "test@test.com", PasswordHash = "hash" };
        _context.Users.Add(user);

        var template = new Template
        {
            Id = 1,
            Name = "Test Template",
            Description = "Test Description",
            Slug = "test-template",
            IsPublic = true,
            UserId = 1,
            LicenseType = "MIT",
            CreatedAt = DateTime.UtcNow
        };

        var dependency = new TemplateDependency
        {
            Id = 1,
            Name = "dep1",
            TemplateId = 1
        };

        var version = new DependencyVersion
        {
            Id = 1,
            Version = "1.0.0",
            TemplateDependencyId = 1
        };

        var command = new DependencyCommand
        {
            Id = 1,
            Command = "echo hello",
            OS = OperatingSystem.Linux,
            Order = 1,
            TemplateDependencyId = 1
        };

        _context.Templates.Add(template);
        _context.TemplateDependencies.Add(dependency);
        _context.Set<DependencyVersion>().Add(version);
        _context.Set<DependencyCommand>().Add(command);

        _context.SaveChanges();
    }

    [Fact]
    public async Task GetRegistry_ReturnsOk_WhenExists()
    {
        var result = await _controller.GetRegistry(1, "test-template");

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        var root = System.Text.Json.JsonDocument.Parse(json).RootElement;

        Assert.Equal("test-template", root.GetProperty("name").GetString());
        Assert.Equal("testuser", root.GetProperty("author").GetString());
        
        var deps = root.GetProperty("dependencies");
        Assert.Equal(1, deps.GetArrayLength());
        Assert.Equal("dep1", deps[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetRegistry_ReturnsNotFound_WhenNotExists()
    {
        var result = await _controller.GetRegistry(1, "non-existent");
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetRegistry_ReturnsNotFound_WhenPrivate()
    {
        // Setup private template
        var privateTemplate = new Template
        {
            Id = 2,
            Name = "Private",
            Description = "Private",
            Slug = "private",
            IsPublic = false,
            UserId = 1,
            LicenseType = "MIT",
            CreatedAt = DateTime.UtcNow
        };
        _context.Templates.Add(privateTemplate);
        _context.SaveChanges();

        var result = await _controller.GetRegistry(1, "private");
        Assert.IsType<NotFoundResult>(result);
    }
}
