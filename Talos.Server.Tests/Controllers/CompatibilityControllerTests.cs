using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Talos.Server.Controllers;
using Talos.Server.Data;
using Talos.Server.Models;
using Talos.Server.Models.Dtos.Compatibility;
using Xunit;

public class CompatibilityControllerTests
{
    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private CompatibilityController CreateController(AppDbContext context)
    {
        var logger = new Mock<ILogger<CompatibilityController>>();
        return new CompatibilityController(context, logger.Object);
    }

   
    // CHECK
    

    [Fact]
    public async Task CheckCompatibility_ReturnsBadRequest_WhenPackagesMissing()
    {
        var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.CheckCompatibility(null, null);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    
    // ANALYZE
    

    [Fact]
    public async Task AnalyzeTemplateCompatibility_ReturnsBadRequest_WhenDtoIsNull()
    {
        var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.AnalyzeTemplateCompatibility(null);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AnalyzeTemplateCompatibility_ReturnsOk_WithValidDependency()
    {
        var context = CreateContext();

        var package = new Package
        {
            Id = 1,
            Name = "Newtonsoft.Json",
            ShortName = "json",
            RepositoryUrl = "https://github.com/JamesNK/Newtonsoft.Json",
            OfficialDocumentationUrl = "https://www.newtonsoft.com/json"
        };

        var version = new PackageVersion
        {
            Id = 1,
            PackageId = 1,
            Version = "13.0.1",
            ReleaseDate = DateTime.UtcNow.AddDays(-30),
            IsDeprecated = false,
            DeprecationMessage = "",
            DownloadUrl = "https://nuget.org/packages/Newtonsoft.Json/13.0.1",
            ReleaseNotesUrl = "https://github.com/JamesNK/Newtonsoft.Json/releases/tag/13.0.1"
        };

        context.Packages.Add(package);
        context.PackageVersions.Add(version);
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var dto = new TemplateAnalysisDto
        {
            Dependencies = new List<PackageDependencyDto>
            {
                new PackageDependencyDto
                {
                    PackageName = "Newtonsoft.Json",
                    Version = "13.0.1"
                }
            }
        };

        var result = await controller.AnalyzeTemplateCompatibility(dto);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

   
    // GET PACKAGE COMPATIBILITIES
   

    [Fact]
    public async Task GetPackageCompatibilities_ReturnsNotFound_WhenPackageDoesNotExist()
    {
        var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.GetPackageCompatibilities(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }


    // BETWEEN
    

    [Fact]
    public async Task GetCompatibilityBetweenPackages_ReturnsNotFound_WhenNoData()
    {
        var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.GetCompatibilityBetweenPackages(1, 2);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    
    // REPORT
    

    [Fact]
    public async Task ReportCompatibilityIssue_ReturnsBadRequest_WhenPackagesMissing()
    {
        var context = CreateContext();
        var controller = CreateController(context);

        var dto = new CompatibilityReportDto
        {
            SourcePackageId = 1,
            TargetPackageId = 2,
            IssueType = "error",
            Description = "Crash al iniciar"
        };

        var result = await controller.ReportCompatibilityIssue(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ReportCompatibilityIssue_ReturnsOk_WhenValid()
    {
        var context = CreateContext();

        var source = new Package 
        { 
            Id = 1, 
            Name = "PackageA",
            ShortName = "pkg-a",
            RepositoryUrl = "http://repo.a",
            OfficialDocumentationUrl = "http://docs.a"
        };
        var target = new Package 
        { 
            Id = 2, 
            Name = "PackageB",
            ShortName = "pkg-b",
            RepositoryUrl = "http://repo.b",
            OfficialDocumentationUrl = "http://docs.b"
        };

        context.Packages.AddRange(source, target);
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var dto = new CompatibilityReportDto
        {
            SourcePackageId = 1,
            TargetPackageId = 2,
            SourceVersion = "1.0.0",
            TargetVersion = "2.0.0",
            IssueType = "crash",
            Description = "Error crítico",
            ReportedBy = "tester"
        };

        var result = await controller.ReportCompatibilityIssue(dto);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }
}
