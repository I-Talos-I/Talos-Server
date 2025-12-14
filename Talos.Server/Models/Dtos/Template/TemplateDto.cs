namespace Talos.Server.Models.Dtos;

public class TemplateDto
{
    public int Id { get; set; }
    public string TemplateName { get; set; }
    public string Slug { get; set; }
    public bool IsPublic { get; set; }
    public string? LicenseType { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UserId { get; set; }
}
