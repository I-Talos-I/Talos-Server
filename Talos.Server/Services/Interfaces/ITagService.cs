using Talos.Server.Models;

namespace Talos.Server.Services.Interfaces;

public interface ITagService
{
    Task<Tag> CreateTagAsync(string name, string? description = null, string? color = null, bool isSystem = false);
    Task<List<Tag>> GetAllTagsAsync();
    Task<Tag?> GetTagByIdAsync(int tagId);
    Task<bool> DeleteTagAsync(int tagId);
}