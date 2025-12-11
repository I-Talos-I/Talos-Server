using System.ComponentModel.DataAnnotations;

public class CompatibilityReportDto
{
    public int? SourcePackageId { get; set; }
    public string? SourcePackageName { get; set; }
    public int? TargetPackageId { get; set; }
    public string? TargetPackageName { get; set; }
    public string? SourceVersion { get; set; }
    public string? TargetVersion { get; set; }
    
    [Required]
    public string IssueType { get; set; } // "crash", "error", "warning", "deprecation", "performance"
    
    [Required]
    [StringLength(500)]
    public string Description { get; set; }
    
    [StringLength(1000)]
    public string? StepsToReproduce { get; set; }
    
    public string? ExpectedBehavior { get; set; }
    public string? ActualBehavior { get; set; }
    public string? Environment { get; set; } // "node 18", "python 3.9", etc.
    public string? ReportedBy { get; set; }
    public string? Priority { get; set; } // "low", "medium", "high", "critical"
}