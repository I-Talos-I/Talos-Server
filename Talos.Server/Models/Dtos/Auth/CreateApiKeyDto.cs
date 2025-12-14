namespace Talos.Server.Models.Dtos.Auth;

public class CreateApiKeyDto
{
    public string Owner { get; set; } = string.Empty;
    public string Role { get; set; } = "user";
    public string? Scope { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? MaxUsage { get; set; }
}