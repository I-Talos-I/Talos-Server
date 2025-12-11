namespace Talos.Server.Models.Dtos
{
    public class TemplateDependencyDto
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public int PackageId { get; set; }
        public string VersionConstraint { get; set; }
        public DateTime CreateAt { get; set; }
    }
}