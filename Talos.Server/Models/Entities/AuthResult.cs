using Talos.Server.Models.Dtos;
using Talos.Server.Models.Dtos.Auth;
using Talos.Server.Models.DTOs.Users;

namespace Talos.Server.Models;

public class AuthResult
{
    public bool Success { get; set; }
    public string Token { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Error { get; set; }
    public UserDto User { get; set; }

    public static AuthResult SuccessResult(string token, string refreshToken, DateTime expiresAt, UserDto user)
    {
        return new AuthResult
        {
            Success = true,
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            User = user
        };
    }

    public static AuthResult Failure(string error)
    {
        return new AuthResult
        {
            Success = false,
            Error = error
        };
    }
}