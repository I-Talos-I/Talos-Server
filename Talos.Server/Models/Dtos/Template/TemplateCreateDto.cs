using System.ComponentModel.DataAnnotations;

namespace Talos.Server.Models.Dtos;

public class TemplateCreateDto
{
    [Required]
    [MinLength(3)]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Url]
    public string? RepositoryUrl { get; set; }

    public bool IsPublic { get; set; } = true;

    [MaxLength(50)]
    public string LicenseType { get; set; } = "MIT";

    [Required]
    [MinLength(1)]
    public List<TemplateDependencyCreateDto> Dependencies { get; set; } = new();
}