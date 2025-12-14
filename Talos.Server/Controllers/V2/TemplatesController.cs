using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Talos.Server.Data;

namespace Talos.Server.Controllers.v2
{
    [ApiController]
    [Route("api/v2/templates")]
    [ApiExplorerSettings(GroupName = "v2")]
    public class TemplateController : ControllerBase
    {
        private readonly AppDbContext _db;

        public TemplateController(AppDbContext db)
        {
            _db = db;
        }
        
        // POST: /api/v2/templates/{id}/tags create tags
       
        [HttpPost("{id:int}/tags")]
        public async Task<IActionResult> AddTags(
            int id,
            [FromBody] List<string> tags)
        {
            var template = await _db.Templates.FindAsync(id);
            if (template == null)
                return NotFound("Template not found");

            tags = tags
                .Select(t => t.Trim().ToLower())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .ToList();

            template.Tags ??= new List<string>();

            foreach (var tag in tags)
            {
                if (!template.Tags.Contains(tag))
                    template.Tags.Add(tag);
            }

            await _db.SaveChangesAsync();
            return Ok(template.Tags);
        }

        // DELETE: /api/v2/templates/{id}/tags/{tag} delete a tag
    
        [HttpDelete("{id:int}/tags/{tag}")]
        public async Task<IActionResult> RemoveTag(int id, string tag)
        {
            var template = await _db.Templates.FindAsync(id);
            if (template == null)
                return NotFound("Template not found");

            tag = tag.Trim().ToLower();

            if (!template.Tags.Remove(tag))
                return NotFound("Tag not found");

            await _db.SaveChangesAsync();
            return NoContent();
        }
        
        //update tags
        
        [HttpPut("{id:int}/tags")]
        public async Task<IActionResult> ReplaceTags(
            int id,
            [FromBody] List<string> tags)
        {
            var template = await _db.Templates.FindAsync(id);
            if (template == null)
                return NotFound("Template not found");

            tags = tags
                .Select(t => t.Trim().ToLower())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .ToList();

            template.Tags = tags;

            await _db.SaveChangesAsync();
            return Ok(template.Tags);
        }


      
        // GET: /api/v2/templates/tag/{tag}  get all templates for tag
        [HttpGet("tag/{tag}")]
        public async Task<IActionResult> GetTemplatesByTag(string tag)
        {
            tag = tag.Trim().ToLower();

            var templates = await _db.Templates
                .Where(t => t.IsPublic && t.Tags.Contains(tag))
                .Select(t => new
                {
                    t.Id,
                    t.TemplateName,
                    t.Slug,
                    t.Tags
                })
                .ToListAsync();

            return Ok(templates);
        }
    }
}
