namespace Talos.Server.Models.Dtos;

public class PreflightRequest
{
    public string SystemInfo { get; set; } = "";
    public string[] RequiredComponents { get; set; } = Array.Empty<string>();
}