using System.ComponentModel.DataAnnotations;

namespace Talos.Server.Models.Entities;

public class ApiKey
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(128)]
    public string Key { get; set; } = string.Empty; // La clave real, generada de forma segura

    [Required]
    [MaxLength(50)]
    public string Owner { get; set; } = string.Empty; // Usuario o sistema propietario de la API key

    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = "user"; // Rol: admin, developer, read-only, etc.

    [Required]
    public bool IsActive { get; set; } = true; // Estado: activo, revocado, expirado

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; } // Fecha de vencimiento opcional

    [MaxLength(200)]
    public string? Scope { get; set; } // Alcance o permisos: "read:users,write:templates"

    public int UsageCount { get; set; } = 0; // Contador básico de uso

    public int? MaxUsage { get; set; } = null; // Límite de uso opcional

    public ICollection<ApiKeyAudit> Audits { get; set; } = new List<ApiKeyAudit>();
}