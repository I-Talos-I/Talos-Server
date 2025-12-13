using Microsoft.EntityFrameworkCore;
using Talos.Server.Data;
using Talos.Server.Models;
using Talos.Server.Services.Interfaces;

namespace Talos.Server.Services;

public class TagService : ITagService
{
    private readonly AppDbContext _context;

    public TagService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Tag> CreateTagAsync(string name, string? description = null, string? color = null, bool isSystem = false)
    {
        var tag = new Tag
        {
            Name = name,
            Description = description,
            Color = color ?? "#3B82F6",
            IsSystemTag = isSystem,
            CreatedAt = DateTime.UtcNow,
            UpdatedKey = DateTime.UtcNow
        };

        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();
        return tag;
    }

    public async Task<List<Tag>> GetAllTagsAsync()
    {
        return await _context.Tags.ToListAsync();
    }

    public async Task<Tag?> GetTagByIdAsync(int tagId)
    {
        return await _context.Tags.FindAsync(tagId);
    }

    public async Task<bool> DeleteTagAsync(int tagId)
    {
        var tag = await _context.Tags.FindAsync(tagId);
        if (tag == null) return false;

        _context.Tags.Remove(tag);
        await _context.SaveChangesAsync();
        return true;
    }
}