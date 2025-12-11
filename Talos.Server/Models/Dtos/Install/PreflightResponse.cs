namespace Talos.Server.Models.Dtos;

public class PreflightResponse
{
    public bool IsValid { get; set; }
    public string[] Warnings { get; set; } = Array.Empty<string>();
    public string[] Errors { get; set; } = Array.Empty<string>();
}