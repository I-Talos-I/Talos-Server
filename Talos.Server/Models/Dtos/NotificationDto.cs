namespace Talos.Server.Models.Dtos;

public class NotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    public int? TagId { get; set; }
    public string? TagName { get; set; }
}