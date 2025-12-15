using Talos.Server.Models;

namespace Talos.Server.Services.Interfaces;

public interface IFollowService
{
    Task<bool> FollowUserAsync(int followerId, int followeeId);
    Task<bool> UnfollowUserAsync(int followerId, int followeeId);
    Task<bool> IsFollowingAsync(int followerId, int followeeId);
    Task<List<User>> GetFollowersAsync(int userId);
    Task<List<User>> GetFollowingAsync(int userId);
}