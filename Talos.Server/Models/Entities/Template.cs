using System.ComponentModel.DataAnnotations;
using Talos.Server.Models;

public class Template
{
    [Key]
    public int Id { get; set; }

    public int? UserId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;

    [Required, MaxLength(120)]
    public string Slug { get; set; } = null!;

    public bool IsPublic { get; set; }

    [MaxLength(50)]
    public string LicenseType { get; set; } = "MIT";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }

    public ICollection<TemplateDependency> Dependencies { get; set; } = new List<TemplateDependency>();
}