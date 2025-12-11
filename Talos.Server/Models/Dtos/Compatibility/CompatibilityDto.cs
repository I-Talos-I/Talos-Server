namespace Talos.Server.Models.Dtos.Compatibility;

public class CompatibilityDto
{
    public int Id { get; set; }
    public string SourceVersion { get; set; }
    public string TargetVersion { get; set; }
    public string TargetVersionConstraint { get; set; }
    public string CompatibilityType { get; set; }
    public int CompatibilityScore { get; set; }
    public string ConfidenceLevel { get; set; }
    public string DetectedBy { get; set; }
    public DateTime DetectionDate { get; set; }
    public bool IsActive { get; set; }
}