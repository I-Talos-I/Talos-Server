namespace Talos.Server.Models.Dtos.Compatibility;

public class TemplateAnalysisDto
{
    public List<PackageDependencyDto> Dependencies { get; set; }
    public string? Environment { get; set; }
    public string? Notes { get; set; }
}