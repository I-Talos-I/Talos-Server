using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Talos.Server.Controllers;
using Talos.Server.Data;
using Talos.Server.Models;
using Talos.Server.Tests.Helpers;
using Xunit;

public class UsersControllerTest
{
    private AppDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);

        var user = new User
        {
            Id = 1,
            Username = "juan",
            Email = "juan@mail.com",
            PasswordHash = "hashed-password", 
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        var templates = new List<Template>
        {
            new Template
            {
                Id = 1,
                TemplateName = "Template 1",
                Slug = "template-1",
                IsPublic = true,
                LicenseType = "MIT",
                CreateAt = DateTime.UtcNow,
                UserId = 1,
                TemplateDependencies = new List<TemplateDependencies>() // ✅
            },
            new Template
            {
                Id = 2,
                TemplateName = "Template 2",
                Slug = "template-2",
                IsPublic = false,
                LicenseType = "GPL",
                CreateAt = DateTime.UtcNow.AddDays(-1),
                UserId = 1,
                TemplateDependencies = new List<TemplateDependencies>() // ✅
            }
        };

        context.Users.Add(user);
        context.Templates.AddRange(templates);

        context.SaveChanges();

        return context;
    }

}
