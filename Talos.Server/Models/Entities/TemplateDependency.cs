using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Talos.Server.Models.Entities;


public class TemplateDependency
{
    [Key]
    public int Id { get; set; }

    public int TemplateId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = null!; // docker engine

    public ICollection<DependencyVersion> Versions { get; set; } = new List<DependencyVersion>();

    public ICollection<DependencyCommand> Commands { get; set; } = new List<DependencyCommand>();

    public Template Template { get; set; } = null!;
}