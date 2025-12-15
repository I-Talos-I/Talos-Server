using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Package
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; }
    public string ShortName { get; set; }
    public string RepositoryUrl { get; set; }
    public string OfficialDocumentationUrl { get; set; }
    public DateTime? LastScrapedAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreateAt { get; set; }
    public DateTime UpdateAt { get; set; }

    [ForeignKey("PackageManager")]
    public int PackageManagerId { get; set; }
    public PackageManager PackageManager { get; set; }

    public ICollection<PackageVersion> PackageVersions { get; set; }
    public ICollection<TemplateDependency> TemplateDependencies { get; set; }
}