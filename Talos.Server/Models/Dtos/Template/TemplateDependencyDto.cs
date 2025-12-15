namespace Talos.Server.Models.Dtos;

public class TemplateDependencyDto
{
    public string Name { get; set; } = null!;
    public IReadOnlyList<string> Versions { get; set; } = Array.Empty<string>();
    public DependencyCommandsDto Commands { get; set; } = new();
}