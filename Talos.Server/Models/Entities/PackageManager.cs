using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class PackageManager
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }

    [JsonIgnore] // evita ciclo al serializar PackageManager - Packages - PackageManager
    public ICollection<Package> Packages { get; set; }
}
