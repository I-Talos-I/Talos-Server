using System.ComponentModel.DataAnnotations;

namespace Talos.Server.Models.DTOs.Auth;

public class LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    public string Password { get; set; }
}