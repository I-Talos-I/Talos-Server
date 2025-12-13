using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Talos.Server.Models;

[Table("UserNotificationPreference")]
public class UserNotificationPreference
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int TagId { get; set; }

    public bool ViaEmail { get; set; } = true;

    public bool ViaPush { get; set; } = true;

    public bool ViaWeb { get; set; } = true;

    public bool IsMuted { get; set; } = false;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdateAt { get; set; }

    // Navegación
    public User User { get; set; }
    public Tag Tag { get; set; }
}