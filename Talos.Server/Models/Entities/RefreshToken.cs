using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Talos.Server.Models.Entities;

[Table("RefreshToken")]
public class RefreshToken
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string Token { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime ExpiresAt { get; set; }
    
    public bool IsRevoked { get; set; } = false;

    public DateTime? RevokedAt { get; set; }
    
    [Required]
    public int UserId { get; set; }
    public User User { get; set; }
}