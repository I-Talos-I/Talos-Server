using System.ComponentModel.DataAnnotations;

namespace Talos.Server.Models.Dtos;

public class TemplateUpdateDto
{
    [MinLength(3)]
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [Url]
    public string? RepositoryUrl { get; set; }

    public bool? IsPublic { get; set; }

    [MaxLength(50)]
    public string? LicenseType { get; set; }
}