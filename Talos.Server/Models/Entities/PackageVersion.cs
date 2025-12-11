using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class PackageVersion
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("Package")]
    public int PackageId { get; set; }
    public Package Package { get; set; }

    public string Version { get; set; }
    public DateTime ReleaseDate { get; set; }
    public bool IsDeprecated { get; set; }
    public string DeprecationMessage { get; set; }
    public string DownloadUrl { get; set; }
    public string ReleaseNotesUrl { get; set; }
    public DateTime CreateAt { get; set; }

    public ICollection<Compatibility> SourceCompatibilities { get; set; }
    public ICollection<Compatibility> TargetCompatibilities { get; set; }
}