using Talos.Server.Models.Dtos;

namespace Talos.Server.Services.Interfaces;

public interface IUserStatusService
{
    Task SetUserOnlineAsync(int userId);
    Task SetUserOfflineAsync(int userId);
    Task<bool> IsUserOnlineAsync(int userId);
    Task<List<UserStatusDto>> GetUsersStatusAsync();
}