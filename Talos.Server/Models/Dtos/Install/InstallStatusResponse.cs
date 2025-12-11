namespace Talos.Server.Models.Dtos;

public class InstallStatusResponse
{
    public string InstallId { get; set; } = "";
    public string Status { get; set; } = "";
    public int Progress { get; set; }
    public string[] Errors { get; set; } = Array.Empty<string>();
    public List<string> Logs { get; set; } = new();
}