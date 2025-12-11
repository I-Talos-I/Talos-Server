using System.Threading.Tasks;
using Talos.Server.Models.DTOs.Auth;

namespace Talos.Server.Services.Auth
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(string email, string password);
        Task<AuthResponseDto> RegisterAsync(UserRegisterDto registerDto);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
    }
}