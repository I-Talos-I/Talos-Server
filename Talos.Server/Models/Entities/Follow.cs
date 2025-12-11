using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Talos.Server.Models;

public class Follow
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("FollowingUser")]
    public int FollowingUserId { get; set; }

    [ForeignKey("FollowedUser")]
    public int FollowedUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public User FollowingUser { get; set; }
    public User FollowedUser { get; set; }
}