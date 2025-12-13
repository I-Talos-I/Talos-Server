using System.ComponentModel.DataAnnotations;

namespace Talos.Server.Models.Dtos.Package;

public class PackageSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string ShortName { get; set; }
    public PackageManagerDto? Manager { get; set; }
    public string RepositoryUrl { get; set; }
    public string OfficialDocumentationUrl { get; set; }
    public bool IsActive { get; set; }
    public int VersionsCount { get; set; }
    public int DependenciesCount { get; set; }
    public DateTime? LastScrapedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PackageManagerDto
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class PackagesPageDto
{
    
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public List<PackageSummaryDto> Packages { get; set; } = new();
}

public class PackagesByManagerDto
{
    public int Id { get; set; }
    public PackageManagerDto Manager { get; set; } = new();
    public int Total { get; set; }
    public List<PackageSummaryDto> Packages { get; set; } = new();
}
public class PackageDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string ShortName { get; set; }
    public string RepositoryUrl { get; set; }
    public string OfficialDocumentationUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreateAt { get; set; }
    public DateTime UpdateAt { get; set; }

    public string PackageManagerName { get; set; }
}

public class PackageCreateDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; }

    [StringLength(100)]
    public string ShortName { get; set; }

    [Url]
    public string? RepositoryUrl { get; set; }

    [Url]
    public string? OfficialDocumentationUrl { get; set; }

    [Required]
    public int PackageManagerId { get; set; }

    public bool IsActive { get; set; } = true;
}

