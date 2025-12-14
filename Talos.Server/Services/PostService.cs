using Microsoft.EntityFrameworkCore;
using Talos.Server.AI;
using Talos.Server.Data;
using Talos.Server.Models;
using Talos.Server.Models.Entities;
using Talos.Server.Services.Interfaces;

namespace Talos.Server.Services;

public class PostService : IPostService
{
    private readonly AppDbContext _context;
    private readonly AiTagService _aiTagService;

    public PostService(
        AppDbContext context,
        AiTagService aiTagService
    )
    {
        _context = context;
        _aiTagService = aiTagService;
    }

    public async Task<Post> CreatePostAsync(
        int userId,
        string title,
        string body,
        string status,
        List<int>? tagIds = null
    )
    {
        var post = new Post
        {
            UserId = userId,
            Title = title,
            Body = body,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

        // Tags manuales
        if (tagIds != null && tagIds.Any())
        {
            var tags = await _context.Tags
                .Where(t => tagIds.Contains(t.Id))
                .ToListAsync();

            foreach (var tag in tags)
                post.Tags.Add(tag);
        }
        // Tags automáticos con IA
        else
        {
            var aiTags = await _aiTagService.GenerateTagsAsync(
                $"{title}. {body}"
            );

            foreach (var tagName in aiTags)
            {
                var existingTag = await _context.Tags
                    .FirstOrDefaultAsync(t => t.Name == tagName);

                if (existingTag == null)
                {
                    existingTag = new Tag
                    {
                        Name = tagName,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Tags.Add(existingTag);
                }

                post.Tags.Add(existingTag);
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
        var followingIds = await _context.Follows
            .Where(f => f.FollowingUserId == userId)
            .Select(f => f.FollowedUserId)
            .ToListAsync();

        followingIds.Add(userId);

        return await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Tags)
            .Where(p => followingIds.Contains(p.UserId))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }
}
