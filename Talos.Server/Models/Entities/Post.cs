using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Talos.Server.Models;

public class Post
{
    [Key]
    public int Id { get; set; }

    public string Title { get; set; }
    public string Body { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }

    [ForeignKey("User")]
    public int UserId { get; set; }
    public User User { get; set; }
}