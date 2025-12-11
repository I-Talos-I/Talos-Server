using Talos.Server.Models.Dtos.Auth;
using Talos.Server.Models.DTOs.Users;


namespace Talos.Server.Models.DTOs.Auth;

public class AuthResponseDto
{
    public bool Success { get; set; }
    public string Token { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Error { get; set; }
    public UserDto User { get; set; }
}