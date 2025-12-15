using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Talos.Server.Controllers.V2; // Namespace might be V2 or just Controllers depending on file, file says public class ApiKeyAdminController without namespace block but file path is V2. 
// Wait, the file content showed:
// using ...
// [ApiController] ... public class ApiKeyAdminController
// It didn't show a namespace declaration! This means it's in the global namespace or default namespace of the project?
// Actually, looking at the file content again:
// 1: using ...
// 7: [ApiController]
// 9: public class ApiKeyAdminController
// It seems it doesn't have a namespace declaration. This is unusual. 
// However, usually it defaults to project name if not specified? No, in C# if no namespace, it's global.
// But wait, `RegistryController.cs` had `namespace Talos.Server.Controllers.V1;`.
// Let's check `ApiKeyAdminController.cs` again. It might be I missed it or it's really missing.
// If it's missing, I should probably add it or use it as global.
// Let's assume it's global for now, or check if I can find it via type search.
// Actually, I'll just try to use it. If it fails, I'll fix it.
// But wait, I should check the file content again carefully.
// The file content I saw:
// 1: using ...
// 6: 
// 7: [ApiController]
// ...
// It really seems to have no namespace.
// I will wrap my test in `Talos.Server.Tests.Controllers` namespace.

using Talos.Server.Data;
using Talos.Server.Models.Dtos.Auth;
using Talos.Server.Models.Entities;
using Xunit;

namespace Talos.Server.Tests.Controllers;

public class ApiKeyAdminControllerTests
{
    private readonly AppDbContext _context;
    private readonly ApiKeyAdminController _controller;

    public ApiKeyAdminControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _controller = new ApiKeyAdminController(_context);

        SeedData();
    }

    private void SeedData()
    {
        _context.ApiKeys.Add(new ApiKey
        {
            Id = 1,
            Key = "test-key",
            Owner = "test",
            Role = "admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var result = await _controller.GetAll();
        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<ApiKey>>(ok.Value);
        Assert.Single(list);
    }

    [Fact]
    public async Task Create_ReturnsOk()
    {
        var dto = new CreateApiKeyDto
        {
            Owner = "new-owner",
            Role = "user",
            Scope = "read",
            MaxUsage = 100
        };

        var result = await _controller.Create(dto);
        var ok = Assert.IsType<OkObjectResult>(result);
        var key = Assert.IsType<ApiKey>(ok.Value);
        
        Assert.NotNull(key.Key);
        Assert.Equal("new-owner", key.Owner);
    }

    [Fact]
    public async Task Revoke_ReturnsOk()
    {
        var result = await _controller.Revoke(1);
        var ok = Assert.IsType<OkObjectResult>(result);
        var key = Assert.IsType<ApiKey>(ok.Value);
        
        Assert.False(key.IsActive);
    }

    [Fact]
    public async Task Revoke_ReturnsNotFound()
    {
        var result = await _controller.Revoke(999);
        Assert.IsType<NotFoundResult>(result);
    }
}
