using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Moq;
using StackExchange.Redis;
using Talos.Server.Controllers;
using Talos.Server.Models.Dtos;
using Xunit;

public class InstallControllerTests
{
    private readonly Mock<IConnectionMultiplexer> _redisMock;
    private readonly Mock<IDatabase> _dbMock;
    private readonly InstallController _controller;

    public InstallControllerTests()
    {
        _redisMock = new Mock<IConnectionMultiplexer>();
        _dbMock = new Mock<IDatabase>();

        _redisMock
            .Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_dbMock.Object);

        _controller = new InstallController(_redisMock.Object);
    }

    // ---------------- PRE-FLIGHT ----------------

    [Fact]
    public void Preflight_ReturnsOk_WhenDockerExists()
    {
        var request = new PreflightRequest
        {
            RequiredComponents = new[] { "Docker", "Git" }
        };

        var result = _controller.Preflight(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PreflightResponse>(ok.Value);

        Assert.True(response.IsValid);
        Assert.Empty(response.Errors);
    }

    [Fact]
    public void Preflight_ReturnsError_WhenDockerMissing()
    {
        var request = new PreflightRequest
        {
            RequiredComponents = new[] { "Git" }
        };

        var result = _controller.Preflight(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PreflightResponse>(ok.Value);

        Assert.False(response.IsValid);
        Assert.Contains("Docker no está instalado.", response.Errors);
    }

    // ---------------- TEMPLATE SCRIPT ----------------

    [Fact]
    public void GetTemplateScript_ReturnsScript()
    {
        var result = _controller.GetTemplateScript(10);

        var ok = Assert.IsType<OkObjectResult>(result);
        var value = ok.Value!.ToString();

        Assert.Contains("Instalando template 10", value);
    }

    // ---------------- START INSTALL ----------------

    [Fact]
    public async Task StartInstallation_CreatesInstallId()
    {
        _dbMock
            .Setup(db => db.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                null,
                false,
                When.Always,
                CommandFlags.None))
            .ReturnsAsync(true);

        var result = await _controller.StartInstallation();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    // ---------------- GET STATUS ----------------

    [Fact]
    public async Task GetInstallStatus_ReturnsStatus_WhenExists()
    {
        var status = new InstallStatusResponse
        {
            InstallId = "123",
            Status = "InProgress",
            Progress = 50,
            Logs = new(),
            Errors = Array.Empty<string>()
        };

        _dbMock
            .Setup(db => db.StringGetAsync(
                "install:123",
                CommandFlags.None))
            .ReturnsAsync(JsonSerializer.Serialize(status));

        var result = await _controller.GetInstallStatus("123");

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<InstallStatusResponse>(ok.Value);

        Assert.Equal(50, response.Progress);
    }

    [Fact]
    public async Task GetInstallStatus_ReturnsNotFound_WhenMissing()
    {
        _dbMock
            .Setup(db => db.StringGetAsync(
                It.IsAny<RedisKey>(),
                CommandFlags.None))
            .ReturnsAsync(RedisValue.Null);

        var result = await _controller.GetInstallStatus("no-existe");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ---------------- LOG INSTALL ----------------

    [Fact]
    public async Task LogInstall_UpdatesProgressAndLogs()
    {
        var status = new InstallStatusResponse
        {
            InstallId = "abc",
            Status = "InProgress",
            Progress = 90,
            Logs = new(),
            Errors = Array.Empty<string>()
        };

        _dbMock
            .Setup(db => db.StringGetAsync(
                "install:abc",
                CommandFlags.None))
            .ReturnsAsync(JsonSerializer.Serialize(status));

        _dbMock
            .Setup(db => db.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                null,
                false,
                When.Always,
                CommandFlags.None))
            .ReturnsAsync(true);

        var log = new InstallLogRequest
        {
            InstallId = "abc",
            Timestamp = DateTime.UtcNow,
            Level = "INFO",
            Message = "Paso final"
        };

        var result = await _controller.LogInstall(log);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task LogInstall_ReturnsNotFound_WhenInstallMissing()
    {
        _dbMock
            .Setup(db => db.StringGetAsync(
                It.IsAny<RedisKey>(),
                CommandFlags.None))
            .ReturnsAsync(RedisValue.Null);

        var log = new InstallLogRequest
        {
            InstallId = "x",
            Timestamp = DateTime.UtcNow,
            Level = "ERROR",
            Message = "Fallo"
        };

        var result = await _controller.LogInstall(log);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
