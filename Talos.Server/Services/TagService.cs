using Microsoft.EntityFrameworkCore;
using Talos.Server.Data;
using Talos.Server.Models;

namespace Talos.Server.Services
{
    public class TagService
    {
        private readonly AppDbContext _db;

        public TagService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<string>> AddTagsAsync(
            int templateId,
            List<string> tags)
        {
            var template = await _db.Templates.FindAsync(templateId);
            if (template == null)
                throw new Exception("Template not found");

            tags = Normalize(tags);

            template.Tags ??= new List<string>();

            foreach (var tag in tags)
            {
                if (!template.Tags.Contains(tag))
                    template.Tags.Add(tag);
            }

            await _db.SaveChangesAsync();
            return template.Tags;
        }

        public async Task RemoveTagAsync(int templateId, string tag)
        {
            var template = await _db.Templates.FindAsync(templateId);
            if (template == null)
                throw new Exception("Template not found");

            tag = tag.Trim().ToLower();

            if (!template.Tags.Remove(tag))
                throw new Exception("Tag not found");

            await _db.SaveChangesAsync();
        }
        public async Task<List<string>> ReplaceTagsAsync(
            int templateId,
            List<string> tags)
        {
            var template = await _db.Templates.FindAsync(templateId);
            if (template == null)
                throw new Exception("Template not found");

            template.Tags = Normalize(tags);

            await _db.SaveChangesAsync();
            return template.Tags;
        }


        public async Task<List<Template>> GetTemplatesByTagAsync(string tag)
        {
            tag = tag.Trim().ToLower();

            return await _db.Templates
                .Where(t => t.IsPublic && t.Tags.Contains(tag))
                .ToListAsync();
        }

        private static List<string> Normalize(List<string> tags)
        {
            return tags
                .Select(t => t.Trim().ToLower())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .ToList();
        }
    }
}