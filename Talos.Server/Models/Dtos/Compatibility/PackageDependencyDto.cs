namespace Talos.Server.Models.Dtos.Compatibility;

public class PackageDependencyDto
{
    public string PackageName { get; set; }
    public string? Version { get; set; }
    public string? VersionConstraint { get; set; }
}