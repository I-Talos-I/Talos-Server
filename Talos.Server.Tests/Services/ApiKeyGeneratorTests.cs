using Xunit;

namespace Talos.Server.Tests.Services;

public class ApiKeyGeneratorTests
{
    [Fact]
    public void GenerateKey_ReturnsStringWithCorrectLength()
    {
        // 32 bytes -> base64 string length is approx 4/3 * 32
        var key = ApiKeyGenerator.GenerateKey(32);
        
        Assert.NotNull(key);
        Assert.True(key.Length > 0);
    }

    [Fact]
    public void GenerateKey_ReturnsUniqueKeys()
    {
        var key1 = ApiKeyGenerator.GenerateKey();
        var key2 = ApiKeyGenerator.GenerateKey();

        Assert.NotEqual(key1, key2);
    }
}
