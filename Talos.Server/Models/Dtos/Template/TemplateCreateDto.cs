using System.ComponentModel.DataAnnotations;

namespace Talos.Server.Models.Dtos
{
    public class TemplateCreateDto
    {
        [Required(ErrorMessage = "Template name is required")]
        [MinLength(3, ErrorMessage = "Name must be at least 3 characters")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Template_Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Url(ErrorMessage = "Repository_Url must be a valid URL")]
        public string? Repository_Url { get; set; }

        public string? Tags { get; set; }
        public string? Category { get; set; }

        public bool Is_Public { get; set; } = true;
        public bool Is_Featured { get; set; } = false;

        [Required]
        [MinLength(1)]
        public string Version { get; set; } = "1.0.0";

        public string? Author_Name { get; set; }

        [Required]
        public string License_Type { get; set; } = "MIT";
    }
}