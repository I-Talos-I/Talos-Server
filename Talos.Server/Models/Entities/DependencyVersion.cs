namespace Talos.Server.Models.Entities;

using System.ComponentModel.DataAnnotations;

public class DependencyVersion
{
    [Key]
    public int Id { get; set; }

    public int TemplateDependencyId { get; set; }

    [Required, MaxLength(20)]
    public string Version { get; set; } = null!;

    public TemplateDependency Dependency { get; set; } = null!;
}
