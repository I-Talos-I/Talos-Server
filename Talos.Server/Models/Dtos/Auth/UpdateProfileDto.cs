using System.ComponentModel.DataAnnotations;

namespace Talos.Server.Models.DTOs.Auth;

public class UpdateProfileDto
{
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; }

    [EmailAddress]
    public string Email { get; set; }

    public string CurrentPassword { get; set; }

    [StringLength(100, MinimumLength = 6)]
    public string NewPassword { get; set; }

    [Compare("NewPassword")]
    public string ConfirmNewPassword { get; set; }
}