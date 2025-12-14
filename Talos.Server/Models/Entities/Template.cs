using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Talos.Server.Models;

public class Template
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("User")]
    public int UserId { get; set; }

    public string TemplateName { get; set; }
    public string Slug { get; set; }
    public bool IsPublic { get; set; }
    public string LicenseType { get; set; }
    public DateTime CreateAt { get; set; }

    public User User { get; set; }
    public List<string> Tags { get; set; } = new();
    public ICollection<TemplateDependencies> TemplateDependencies { get; set; }
}