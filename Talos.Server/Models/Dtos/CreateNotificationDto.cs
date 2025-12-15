namespace Talos.Server.Models.Dtos;

public class CreateNotificationDto
{
    public int UserId { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public int? TagId { get; set; }
}