using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Talos.Server.Models.Entities;

namespace Talos.Server.Models;

[Table("Users")] // Especificar nombre de tabla explícitamente
public class User
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("Username")]
    public string UserName { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    [Column("Email")]
    public string Email { get; set; }

    [Required]
    [Column("PasswordHash")]
    public string PasswordHash { get; set; }
    
    [Column("AvatarUrl")]
    public string? AvatarUrl { get; set; }
    
    [Column("SignalrConnectionId")]
    public string? SignalrConnectionId { get; set; }
    
    [Column("LastConnectionId")]
    public string? LastConnectionId { get; set; }
    
    [Column("IsOnline")]
    public bool IsOnline { get; set; } = false;
    
    [Column("LastSeenAt")]
    public DateTime? LastSeenAt { get; set; }
    
    [MaxLength(45)]
    [Column("LastIpAddress")]
    public string? LastIpAddress { get; set; }
    
    [MaxLength(20)]
    [Column("Tier")]
    public string Tier { get; set; } = "free";
    
    [Column("PrivateTemplateLimit")]
    public int PrivateTemplateLimit { get; set; } = 2;

    [Column("CurrentPrivateTemplates")]
    public int CurrentPrivateTemplates { get; set; } = 0;

    [Column("EmailVerified")]
    public bool EmailVerified { get; set; } = false;
    
    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }
    
    [Column("UpdatedAt")]
    public DateTime UpdatedAt { get; set; }

    [Column("DeletedAt")]
    public DateTime? DeletedAt { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("Role")]
    public string Role { get; set; } = "user";

    // Propiedades de navegación
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<Follow> Followers { get; set; } = new List<Follow>();
    public ICollection<Follow> Following { get; set; } = new List<Follow>();
    public ICollection<Template> Templates { get; set; } = new List<Template>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

}