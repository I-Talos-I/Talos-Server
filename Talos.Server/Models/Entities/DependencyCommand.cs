namespace Talos.Server.Models.Entities;

using System.ComponentModel.DataAnnotations;

public enum OperatingSystem
{
    Linux,
    Windows,
    MacOS
}

public class DependencyCommand
{
    [Key]
    public int Id { get; set; }

    public int TemplateDependencyId { get; set; }

    public OperatingSystem OS { get; set; }

    [Required]
    public string Command { get; set; } = null!;

    public int Order { get; set; } // mantiene el orden del JSON

    public TemplateDependency Dependency { get; set; } = null!;
}