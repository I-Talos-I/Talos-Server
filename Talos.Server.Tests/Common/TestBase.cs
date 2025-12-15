using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Talos.Server.Data;

namespace Talos.Server.Tests.Common;

public abstract class TestBase : IDisposable
{
    protected readonly AppDbContext Context;
    protected readonly IConfiguration Configuration;
    protected readonly Mock<ILogger> LoggerMock;

    protected TestBase()
    {
        // DbContext InMemory
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        Context = new AppDbContext(options);

        // IConfiguration mock (Jwt)
        var settings = new Dictionary<string, string>
        {
            { "JwtSettings:Key", "TEST_KEY_123456789_TEST_KEY_123456789" },
            { "JwtSettings:Issuer", "Talos.Test" },
            { "JwtSettings:Audience", "Talos.Test.Client" }
        };

        Configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings!)
            .Build();

        LoggerMock = new Mock<ILogger>();
    }

    public void Dispose()
    {
        Context.Database.EnsureDeleted();
        Context.Dispose();
    }
}