using Microsoft.EntityFrameworkCore;
using Talos.Server.Data;
using Talos.Server.Models;
using Talos.Server.Models.Entities;
using Talos.Server.Services.Interfaces;

namespace Talos.Server.Services;

public class PostService : IPostService
{
    private readonly AppDbContext _context;

    public PostService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Post> CreatePostAsync(int userId, string title, string body, string status, List<int>? tagIds = null)
    {
        var post = new Post
        {
            UserId = userId,
            Title = title,
            Body = body,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

        if (tagIds != null && tagIds.Any())
        {
            var tags = await _context.Tags.Where(t => tagIds.Contains(t.Id)).ToListAsync();
            foreach (var tag in tags)
            {
                post.Tags.Add(tag);
            }
        }

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();
        return post;
    }

    public async Task<List<Post>> GetUserPostsAsync(int userId)
    {
        return await _context.Posts
            .Include(p => p.Tags)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Post>> GetFeedAsync(int userId)
    {
        // Posts de usuarios que sigo + míos
        var followingIds = await _context.Follows
            .Where(f => f.FollowingUserId == userId)
            .Select(f => f.FollowedUserId)
            .ToListAsync();

        followingIds.Add(userId); // incluir propios posts

        return await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Tags)
            .Where(p => followingIds.Contains(p.UserId))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }
}