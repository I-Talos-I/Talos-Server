using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class TemplateDependencies
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("Template")]
    public int TemplateId { get; set; }

    [ForeignKey("Package")]
    public int PackageId { get; set; }

    public string VersionConstraint { get; set; }
    public DateTime CreateAt { get; set; }

    public Template Template { get; set; }
    public Package Package { get; set; }
}