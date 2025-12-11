using System.ComponentModel.DataAnnotations;

namespace Talos.Server.Models.Dtos.Auth
{
    public class RegisterDto
    {
        [Required]
        public string UserName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}