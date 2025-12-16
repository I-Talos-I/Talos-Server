using Microsoft.EntityFrameworkCore;
using Talos.Server.Data;
using Talos.Server.Models;
using Talos.Server.Services;
using Xunit;

namespace Talos.Server.Tests.Services;

public class TemplateServiceTests
{
    private readonly AppDbContext _context;
    private readonly TemplateService _service;

    public TemplateServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _service = new TemplateService(_context);

        SeedData();
    }

    private void SeedData()
    {
        var user = new User { Id = 1, Username = "testuser", Email = "test@test.com", PasswordHash = "hash" };
        _context.Users.Add(user);

        _context.Templates.AddRange(
            new Template
            {
                Id = 1,
                Name = "Public Template",
                Description = "Public Description",
                Slug = "public-template",
                IsPublic = true,
                UserId = 1,
                LicenseType = "MIT",
                CreatedAt = DateTime.UtcNow
            },
            new Template
            {
                Id = 2,
                Name = "Private Template",
                Description = "Private Description",
                Slug = "private-template",
                IsPublic = false,
                UserId = 1,
                LicenseType = "MIT",
                CreatedAt = DateTime.UtcNow
            }
        );

        _context.SaveChanges();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsTemplate_WhenExists()
    {
        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Public Template", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUserAndSlugAsync_ReturnsTemplate_WhenPrivateAndMatches()
    {
        var result = await _service.GetByUserAndSlugAsync(1, "private-template");

        Assert.NotNull(result);
        Assert.Equal("Private Template", result.Name);
    }

    [Fact]
    public async Task GetByUserAndSlugAsync_ReturnsNull_WhenPublic()
    {
        // The service method filters for !t.IsPublic
        var result = await _service.GetByUserAndSlugAsync(1, "public-template");

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_AddsTemplate()
    {
        var newTemplate = new Template
        {
            Name = "New Template",
            Slug = "new-template",
            IsPublic = true,
            UserId = 1,
            LicenseType = "MIT",
            CreatedAt = DateTime.UtcNow,
            Description = "Description"
        };

        var result = await _service.CreateAsync(newTemplate);

        Assert.NotNull(result);
        Assert.NotEqual(0, result.Id);
        Assert.Equal("New Template", result.Name);

        var inDb = await _context.Templates.FindAsync(result.Id);
        Assert.NotNull(inDb);
    }
}
