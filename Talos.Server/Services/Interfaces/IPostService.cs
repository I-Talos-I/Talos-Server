using Talos.Server.Models.Entities;

namespace Talos.Server.Services.Interfaces;

public interface IPostService
{
    Task<Post> CreatePostAsync(int userId, string title, string body, string status, List<int>? tagIds = null);
    Task<List<Post>> GetUserPostsAsync(int userId);
    Task<List<Post>> GetFeedAsync(int userId);
}