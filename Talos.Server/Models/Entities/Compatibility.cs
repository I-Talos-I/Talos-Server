using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Compatibility
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("SourcePackageVersion")]
    public int SourcePackageVersionId { get; set; }

    [ForeignKey("TargetPackageVersion")]
    public int TargetPackageVersionId { get; set; }

    public string TargetVersionConstraint { get; set; }
    public string CompatibilityType { get; set; }
    public int CompatibilityScore { get; set; }
    public string ConfidenceLevel { get; set; }
    public string DetectedBy { get; set; }
    public DateTime DetectionDate { get; set; }
    public string Notes { get; set; }
    public bool IsActive { get; set; }

    public PackageVersion SourcePackageVersion { get; set; }
    public PackageVersion TargetPackageVersion { get; set; }
}