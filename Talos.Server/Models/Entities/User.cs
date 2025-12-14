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
    public string Username { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    [Column("Email")]
    public string Email { get; set; }  // ¡FALTA ESTE CAMPO!

    [Required]
    [Column("PasswordHash")]
    public string PasswordHash { get; set; }  // ¡FALTA ESTE CAMPO!

    [Required]
    [MaxLength(50)]
    [Column("Role")]
    public string Role { get; set; } = "user";

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    // Propiedades de navegación
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<Follow> Followers { get; set; } = new List<Follow>();
    public ICollection<Follow> Following { get; set; } = new List<Follow>();
    public ICollection<Template> Templates { get; set; } = new List<Template>();
}