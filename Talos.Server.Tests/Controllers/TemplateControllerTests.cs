using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Talos.Server.Controllers;
using Talos.Server.Data;
using Talos.Server.Models;
using Talos.Server.Models.Dtos;
using Xunit;

namespace Talos.Server.Tests.Controllers;

public class TemplateControllerTests
{
    private readonly AppDbContext _context;
    private readonly TemplateController _controller;
    private readonly IMapper _mapper;

    public TemplateControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _mapper = CreateMapper();

        SeedData();

        _controller = new TemplateController(_context, _mapper);

        SetAuthenticatedUser();
    }

    // ---------------- HELPERS ----------------

    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Template, TemplateDto>();
            cfg.CreateMap<TemplateCreateDto, Template>()
                .ForMember(d => d.TemplateName, o => o.MapFrom(s => s.Template_Name))
                .ForMember(d => d.IsPublic, o => o.MapFrom(s => s.Is_Public))
                .ForMember(d => d.LicenseType, o => o.MapFrom(s => s.License_Type));
        });

        return config.CreateMapper();
    }

    private void SetAuthenticatedUser()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "admin")
        };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(claims, "TestAuth"))
            }
        };
    }

    private void SeedData()
    {
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hashedpassword"
        };

        _context.Users.Add(user);

        _context.Users.Add(new User
        {
            Id = 2,
            Username = "otheruser",
            Email = "other@example.com",
            PasswordHash = "hashedpassword"
        });

        _context.Templates.AddRange(
            new Template
            {
                Id = 1,
                TemplateName = "API Base",
                Slug = "api-base",
                IsPublic = true,
                LicenseType = "MIT",
                UserId = 1,
                CreateAt = DateTime.UtcNow
            },
            new Template
            {
                Id = 2,
                TemplateName = "Private Template",
                Slug = "private-template",
                IsPublic = false,
                LicenseType = "Apache",
                UserId = 2,
                CreateAt = DateTime.UtcNow
            }
        );

        _context.SaveChanges();
    }

    // ---------------- GET ALL ----------------

    [Fact]
    public async Task GetAllTemplates_ReturnsOk()
    {
        var result = await _controller.GetAllTemplates(null, null, null);

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<TemplateDto>>(ok.Value);

        Assert.Equal(2, list.Count);
    }

    // ---------------- GET BY USER ----------------

    [Fact]
    public async Task GetTemplatesByUser_ReturnsTemplates()
    {
        var result = await _controller.GetTemplatesByUser(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<TemplateDto>>(ok.Value);

        Assert.Single(list);
        Assert.Equal("API Base", list[0].TemplateName);
    }

    // ---------------- SEARCH ----------------

    [Fact]
    public async Task SearchTemplates_ReturnsBadRequest_WhenEmpty()
    {
        var result = await _controller.SearchTemplates("");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SearchTemplates_ReturnsResults()
    {
        var result = await _controller.SearchTemplates("API");

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<TemplateDto>>(ok.Value);

        Assert.Single(list);
    }

    // ---------------- FEATURED ----------------

    [Fact]
    public async Task GetFeaturedTemplates_ReturnsOnlyPublic()
    {
        var result = await _controller.GetFeaturedTemplates();

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<TemplateDto>>(ok.Value);

        Assert.All(list, t => Assert.NotNull(t));
    }

    // ---------------- GET BY ID ----------------

    [Fact]
    public async Task GetTemplateById_ReturnsTemplate()
    {
        var result = await _controller.GetTemplateById(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<TemplateDto>(ok.Value);

        Assert.Equal("API Base", dto.TemplateName);
    }

    [Fact]
    public async Task GetTemplateById_ReturnsNotFound()
    {
        var result = await _controller.GetTemplateById(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ---------------- CREATE ----------------

    [Fact]
    public async Task CreateTemplate_ReturnsCreated()
    {
        var dto = new TemplateCreateDto
        {
            Template_Name = "New Template",
            Is_Public = true,
            License_Type = "MIT"
        };

        var result = await _controller.CreateTemplate(dto);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var value = Assert.IsType<TemplateDto>(created.Value);

        Assert.Equal("New Template", value.TemplateName);
    }

    [Fact]
    public async Task CreateTemplate_ReturnsConflict_WhenDuplicate()
    {
        var dto = new TemplateCreateDto
        {
            Template_Name = "API Base",
            Is_Public = true
        };

        var result = await _controller.CreateTemplate(dto);

        Assert.IsType<ConflictObjectResult>(result);
    }

    // ---------------- UPDATE ----------------

    [Fact]
    public async Task UpdateTemplate_ReturnsOk()
    {
        var dto = new TemplateCreateDto
        {
            Template_Name = "Updated Template",
            Is_Public = true,
            License_Type = "MIT"
        };

        var result = await _controller.UpdateTemplate(1, dto);

        var ok = Assert.IsType<OkObjectResult>(result);
        var value = Assert.IsType<TemplateDto>(ok.Value);

        Assert.Equal("Updated Template", value.TemplateName);
    }

    [Fact]
    public async Task UpdateTemplate_ReturnsNotFound()
    {
        var dto = new TemplateCreateDto
        {
            Template_Name = "No Exists",
            Is_Public = true
        };

        var result = await _controller.UpdateTemplate(999, dto);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ---------------- DELETE ----------------

    [Fact]
    public async Task DeleteTemplate_ReturnsNoContent()
    {
        var result = await _controller.DeleteTemplate(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteTemplate_ReturnsNotFound()
    {
        var result = await _controller.DeleteTemplate(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
