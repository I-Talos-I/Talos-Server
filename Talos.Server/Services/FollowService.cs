using Microsoft.EntityFrameworkCore;
using Talos.Server.Data;
using Talos.Server.Models;
using Talos.Server.Services.Interfaces;

namespace Talos.Server.Services;

public class FollowService : IFollowService
{
    private readonly AppDbContext _context;

    public FollowService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> FollowUserAsync(int followerId, int followeeId)
    {
        if (followerId == followeeId) return false;

        var exists = await _context.Follows
            .AnyAsync(f => f.FollowingUserId == followerId && f.FollowedUserId == followeeId);

        if (exists) return false;

        var follow = new Follow
        {
            FollowingUserId = followerId,
            FollowedUserId = followeeId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Follows.Add(follow);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnfollowUserAsync(int followerId, int followeeId)
    {
        var follow = await _context.Follows
            .FirstOrDefaultAsync(f => f.FollowingUserId == followerId && f.FollowedUserId == followeeId);

        if (follow == null) return false;

        _context.Follows.Remove(follow);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsFollowingAsync(int followerId, int followeeId)
    {
        return await _context.Follows
            .AnyAsync(f => f.FollowingUserId == followerId && f.FollowedUserId == followeeId);
    }

    public async Task<List<User>> GetFollowersAsync(int userId)
    {
        return await _context.Follows
            .Where(f => f.FollowedUserId == userId)
            .Include(f => f.FollowingUser)
            .Select(f => f.FollowingUser)
            .ToListAsync();
    }

    public async Task<List<User>> GetFollowingAsync(int userId)
    {
        return await _context.Follows
            .Where(f => f.FollowingUserId == userId)
            .Include(f => f.FollowedUser)
            .Select(f => f.FollowedUser)
            .ToListAsync();
    }
}
