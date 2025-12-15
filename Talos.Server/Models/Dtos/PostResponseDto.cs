namespace Talos.Server.Models.Dtos;

public class PostResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public List<string> Tags { get; set; } = new();
}