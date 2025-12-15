using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Talos.Server.Models.Entities;

namespace Talos.Server.Models;

[Table("Tag")]
public class Tag
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    public string? Description { get; set; }

    [MaxLength(7)]
    public string Color { get; set; } = "#3B82F6";

    public bool IsSystemTag { get; set; } = false;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedKey { get; set; }

    // Navegación
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<UserNotificationPreference> UserPreferences { get; set; } = new List<UserNotificationPreference>();
    public ICollection<Post> Posts { get; set; } = new List<Post>();

}