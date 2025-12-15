using System.ComponentModel.DataAnnotations;

namespace Talos.Server.Models.Dtos;

public class TemplateDependencyCreateDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [Required]
    [MinLength(1)]
    public List<string> Versions { get; set; } = new();

    [Required]
    public DependencyCommandsDto Commands { get; set; } = new();
}