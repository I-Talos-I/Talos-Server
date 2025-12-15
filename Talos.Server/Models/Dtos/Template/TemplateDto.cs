namespace Talos.Server.Models.Dtos;

public class TemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public bool IsPublic { get; set; }
    public string LicenseType { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string UserName { get; set; } = null!;
    
    public List<TemplateDependencyDto> Dependencies { get; set; } = new();
}