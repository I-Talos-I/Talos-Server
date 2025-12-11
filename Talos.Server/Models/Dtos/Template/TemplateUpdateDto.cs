
namespace Talos.Server.Models.Dtos;

public class TemplateUpdateDto
{
    public string Template_Name { get; set; }
    public string Description { get; set; }
    public string? Repository_Url { get; set; }
    public string? Tags { get; set; }
    public string? Category { get; set; }
    public bool Is_Public { get; set; } = true;
    public string? Version { get; set; }
    public string? Author_Name { get; set; }
}