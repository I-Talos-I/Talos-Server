using System;
using System.ComponentModel.DataAnnotations;

namespace Talos.Server.Models.Dtos
{
    public class TemplateDependencyCreateDto
    {
        [Required]
        public int TemplateId { get; set; }

        [Required]
        public int PackageId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Version constraint is required")]
        public string VersionConstraint { get; set; }

        public DateTime CreateAt { get; set; } = DateTime.UtcNow;
    }
}
