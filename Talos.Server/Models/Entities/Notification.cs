using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Talos.Server.Models;

[Table("Notification")]
public class Notification
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    public int? TagId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; }

    [Required]
    public string Message { get; set; }

    [Column(TypeName = "json")]
    public string? Payload { get; set; }

    public bool IsRead { get; set; } = false;

    public bool IsArchived { get; set; } = false;

    [MaxLength(20)]
    public string Priority { get; set; } = "medium";

    public DateTime? ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ReadAt { get; set; }

    public DateTime? ArchivedAt { get; set; }

    // Navegación
    public User User { get; set; }
    public Tag? Tag { get; set; }
}