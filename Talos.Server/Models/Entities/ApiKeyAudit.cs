namespace Talos.Server.Models.Entities;

public class ApiKeyAudit
{
    public int Id { get; set; }
    public int ApiKeyId { get; set; }
    public ApiKey ApiKey { get; set; } = null!;
    public string Endpoint { get; set; } = string.Empty;
    public string IP { get; set; } = string.Empty;
    public DateTime AccessedAt { get; set; } = DateTime.UtcNow;
}