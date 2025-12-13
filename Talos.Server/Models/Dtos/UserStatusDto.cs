namespace Talos.Server.Models.Dtos;

public class UserStatusDto
{
    public int UserId { get; set; }
    public string Username { get; set; }
    public bool IsOnline { get; set; }

}