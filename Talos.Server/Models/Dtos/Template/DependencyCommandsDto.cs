using System.ComponentModel.DataAnnotations;

namespace Talos.Server.Models.Dtos;

public class DependencyCommandsDto
{
    public List<string> Linux { get; set; } = new();
    public List<string> Windows { get; set; } = new();
    public List<string> MacOS { get; set; } = new();
}