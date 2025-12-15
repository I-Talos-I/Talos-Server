using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Talos.Server.Controllers;
using Talos.Server.Tests.Helpers;
using Xunit;

public class CacheTestControllerTests
{
    [Fact]
    public async Task TestCache_ReturnsOk_WithCachedValue()
    {
        // Arrange
        var cache = new FakeDistributedCache();
        var controller = new CacheTestController(cache);

        // Act
        var result = await controller.TestCache();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("pruebaaaa", okResult.Value);

        var cachedValue = await cache.GetStringAsync("pruebita");
        Assert.Equal("pruebaaaa", cachedValue);
    }
}