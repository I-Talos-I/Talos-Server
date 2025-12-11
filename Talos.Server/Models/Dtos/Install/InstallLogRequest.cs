namespace Talos.Server.Models.Dtos;

public class InstallLogRequest
{
    public string InstallId { get; set; } = "";
    public string Message { get; set; } = "";
    public string Level { get; set; } = "";
    public DateTime Timestamp { get; set; }
}