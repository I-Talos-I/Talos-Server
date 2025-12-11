using System.ComponentModel.DataAnnotations;
using Talos.Server.Models;

public class PackageManager
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }

    public ICollection<Package> Packages { get; set; }
}