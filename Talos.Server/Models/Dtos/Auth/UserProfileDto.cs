namespace Talos.Server.Models.DTOs.Auth;

public class UserProfileDto
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TemplateCount { get; set; }
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
}