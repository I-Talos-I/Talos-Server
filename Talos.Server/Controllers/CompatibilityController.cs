using System.Collections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Talos.Server.Data;
using Talos.Server.Models;
using Talos.Server.Models.Dtos.Compatibility;

namespace Talos.Server.Controllers;

[ApiController]
[Route("api/compatibility")]
public class CompatibilityController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<CompatibilityController> _logger;

    public CompatibilityController(AppDbContext context, ILogger<CompatibilityController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/compatibility/check
    [HttpGet("check")]
    public async Task<IActionResult> CheckCompatibility(
        [FromQuery] string[] sourcePackages,
        [FromQuery] string[] targetPackages,
        [FromQuery] string[] sourceVersions = null,
        [FromQuery] string[] targetConstraints = null)
    {
        try
        {
            if (sourcePackages == null || sourcePackages.Length == 0 ||
                targetPackages == null || targetPackages.Length == 0)
            {
                return BadRequest(new { message = "Se requieren sourcePackages y targetPackages" });
            }

            var results = new List<object>();

            for (int i = 0; i < Math.Min(sourcePackages.Length, targetPackages.Length); i++)
            {
                var sourcePackage = sourcePackages[i];
                var targetPackage = targetPackages[i];
                var sourceVersion = sourceVersions != null && i < sourceVersions.Length
                    ? sourceVersions[i]
                    : null;
                var targetConstraint = targetConstraints != null && i < targetConstraints.Length
                    ? targetConstraints[i]
                    : null;

                var compatResult = await GetCompatibilityBetweenPackages(
                    sourcePackage,
                    targetPackage,
                    sourceVersion,
                    targetConstraint);

                results.Add(new
                {
                    sourcePackage,
                    targetPackage,
                    sourceVersion,
                    targetConstraint,
                    compatibility = compatResult
                });
            }

            return Ok(new
            {
                timestamp = DateTime.UtcNow,
                totalChecks = results.Count,
                results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en check de compatibilidad múltiple");
            return StatusCode(500, new { message = "Error interno", detail = ex.Message });
        }
    }

    // POST: api/compatibility/analyze
    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeTemplateCompatibility([FromBody] TemplateAnalysisDto analysisDto)
    {
        try
        {
            if (analysisDto == null || analysisDto.Dependencies == null)
            {
                return BadRequest(new { message = "Datos de análisis inválidos" });
            }

            var analysisResults = new List<object>();
            var warnings = new List<string>();
            var errors = new List<string>();

            // Analizar cada dependencia
            foreach (var dep in analysisDto.Dependencies)
            {
                try
                {
                    // Buscar el paquete
                    var package = await _context.Packages
                        .FirstOrDefaultAsync(p => p.Name == dep.PackageName || p.ShortName == dep.PackageName);

                    if (package == null)
                    {
                        warnings.Add($"Paquete no encontrado: {dep.PackageName}");
                        continue;
                    }

                    // Buscar versión específica si se proporciona
                    PackageVersion version = null;
                    if (!string.IsNullOrEmpty(dep.Version))
                    {
                        version = await _context.PackageVersions
                            .FirstOrDefaultAsync(v => v.PackageId == package.Id && v.Version == dep.Version);
                    }

                    // Si no se encontró versión específica, usar la última
                    if (version == null)
                    {
                        version = await _context.PackageVersions
                            .Where(v => v.PackageId == package.Id && !v.IsDeprecated)
                            .OrderByDescending(v => v.ReleaseDate)
                            .FirstOrDefaultAsync();
                    }

                    if (version == null)
                    {
                        warnings.Add($"No se encontró versión para: {dep.PackageName}");
                        continue;
                    }

                    // Verificar compatibilidad con otras dependencias
                    var incompatibilities = await _context.Compatibilities
                        .Include(c => c.SourcePackageVersion)
                        .ThenInclude(v => v.Package)
                        .Include(c => c.TargetPackageVersion)
                        .ThenInclude(v => v.Package)
                        .Where(c => c.SourcePackageVersionId == version.Id ||
                                    c.TargetPackageVersionId == version.Id)
                        .Where(c => c.CompatibilityScore < 80 ||
                                    c.CompatibilityType == "incompatible")
                        .Take(10)
                        .Select(c => new
                        {
                            conflictingPackage = c.SourcePackageVersionId == version.Id
                                ? c.TargetPackageVersion.Package.Name
                                : c.SourcePackageVersion.Package.Name,
                            conflictingVersion = c.SourcePackageVersionId == version.Id
                                ? c.TargetPackageVersion.Version
                                : c.SourcePackageVersion.Version,
                            c.CompatibilityScore,
                            c.CompatibilityType,
                            c.DetectedBy,
                            c.DetectionDate
                        })
                        .ToListAsync();

                    analysisResults.Add(new
                    {
                        package = package.Name,
                        version = version.Version,
                        isDeprecated = version.IsDeprecated,
                        deprecationMessage = version.DeprecationMessage,
                        releaseDate = version.ReleaseDate,
                        daysSinceRelease = (DateTime.UtcNow - version.ReleaseDate).Days,
                        incompatibilities,
                        recommendation = version.IsDeprecated
                            ? "Considerar actualizar a versión no obsoleta"
                            : incompatibilities.Any()
                                ? "Verificar incompatibilidades"
                                : "Compatible"
                    });
                }
                catch (Exception ex)
                {
                    errors.Add($"Error analizando {dep.PackageName}: {ex.Message}");
                }
            }

            // Generar resumen
            var summary = new
            {
                totalDependencies = analysisDto.Dependencies.Count,
                analyzed = analysisResults.Count,
                warnings = warnings.Count,
                errors = errors.Count,
                deprecatedCount = analysisResults.Count(r => (bool)r.GetType().GetProperty("isDeprecated").GetValue(r)),
                incompatibleCount = analysisResults.Count(r =>
                    ((List<object>)r.GetType().GetProperty("incompatibilities").GetValue(r)).Count > 0),
                riskLevel = analysisResults.Any(r =>
                    (bool)r.GetType().GetProperty("isDeprecated").GetValue(r) ||
                    ((List<object>)r.GetType().GetProperty("incompatibilities").GetValue(r)).Count > 0)
                    ? "MEDIUM"
                    : "LOW"
            };

            return Ok(new
            {
                analysisId = Guid.NewGuid(),
                timestamp = DateTime.UtcNow,
                summary,
                details = analysisResults,
                warnings,
                errors,
                recommendations = GenerateRecommendations(analysisResults)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en análisis de compatibilidad");
            return StatusCode(500, new { message = "Error interno", detail = ex.Message });
        }
    }

    // GET: api/compatibility/package/{packageId}
    [HttpGet("package/{packageId}")]
    public async Task<IActionResult> GetPackageCompatibilities(
        int packageId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? compatibilityType = null,
        [FromQuery] int? minScore = null)
    {
        try
        {
            var package = await _context.Packages.FindAsync(packageId);
            if (package == null)
            {
                return NotFound(new { message = "Paquete no encontrado" });
            }

            // Obtener compatibilidades donde el paquete es fuente
            var sourceQuery = _context.Compatibilities
                .Include(c => c.SourcePackageVersion)
                .ThenInclude(v => v.Package)
                .Include(c => c.TargetPackageVersion)
                .ThenInclude(v => v.Package)
                .Where(c => c.SourcePackageVersion.PackageId == packageId);

            // Obtener compatibilidades donde el paquete es destino
            var targetQuery = _context.Compatibilities
                .Include(c => c.SourcePackageVersion)
                .ThenInclude(v => v.Package)
                .Include(c => c.TargetPackageVersion)
                .ThenInclude(v => v.Package)
                .Where(c => c.TargetPackageVersion.PackageId == packageId);

            // Combinar queries
            var combinedQuery = sourceQuery
                .Select(c => new
                {
                    c.Id,
                    direction = "source_to_target",
                    sourcePackage = c.SourcePackageVersion.Package.Name,
                    sourceVersion = c.SourcePackageVersion.Version,
                    targetPackage = c.TargetPackageVersion.Package.Name,
                    targetVersion = c.TargetPackageVersion.Version,
                    c.TargetVersionConstraint,
                    c.CompatibilityType,
                    c.CompatibilityScore,
                    c.ConfidenceLevel,
                    c.DetectedBy,
                    c.DetectionDate,
                    c.IsActive
                })
                .Concat(targetQuery.Select(c => new
                {
                    c.Id,
                    direction = "target_to_source",
                    sourcePackage = c.SourcePackageVersion.Package.Name,
                    sourceVersion = c.SourcePackageVersion.Version,
                    targetPackage = c.TargetPackageVersion.Package.Name,
                    targetVersion = c.TargetPackageVersion.Version,
                    c.TargetVersionConstraint,
                    c.CompatibilityType,
                    c.CompatibilityScore,
                    c.ConfidenceLevel,
                    c.DetectedBy,
                    c.DetectionDate,
                    c.IsActive
                }));

            // Aplicar filtros
            if (!string.IsNullOrEmpty(compatibilityType))
            {
                combinedQuery = combinedQuery.Where(c => c.CompatibilityType == compatibilityType);
            }

            if (minScore.HasValue)
            {
                combinedQuery = combinedQuery.Where(c => c.CompatibilityScore >= minScore.Value);
            }

            var totalItems = await combinedQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var compatibilities = await combinedQuery
                .OrderByDescending(c => c.CompatibilityScore)
                .ThenByDescending(c => c.DetectionDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Estadísticas
            var stats = new
            {
                totalCompatibilities = totalItems,
                averageScore = compatibilities.Any() ? compatibilities.Average(c => c.CompatibilityScore) : 0,
                byType = compatibilities
                    .GroupBy(c => c.CompatibilityType)
                    .Select(g => new { type = g.Key, count = g.Count() })
                    .ToList(),
                byConfidence = compatibilities
                    .GroupBy(c => c.ConfidenceLevel)
                    .Select(g => new { level = g.Key, count = g.Count() })
                    .ToList()
            };

            return Ok(new
            {
                packageId,
                packageName = package.Name,
                page,
                pageSize,
                totalItems,
                totalPages,
                stats,
                compatibilities
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error obteniendo compatibilidades del paquete {packageId}");
            return StatusCode(500, new { message = "Error interno", detail = ex.Message });
        }
    }

    // GET: api/compatibility/between
    [HttpGet("between")]
    public async Task<IActionResult> GetCompatibilityBetweenPackages(
        [FromQuery] int sourcePackageId,
        [FromQuery] int targetPackageId,
        [FromQuery] string? sourceVersion = null,
        [FromQuery] string? targetVersionConstraint = null)
    {
        try
        {
            var result = await GetCompatibilityBetweenPackages(
                sourcePackageId.ToString(),
                targetPackageId.ToString(),
                sourceVersion,
                targetVersionConstraint,
                true);

            if (result == null)
            {
                return NotFound(new
                {
                    message = "No se encontró información de compatibilidad",
                    sourcePackageId,
                    targetPackageId
                });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo compatibilidad entre paquetes");
            return StatusCode(500, new { message = "Error interno", detail = ex.Message });
        }
    }

    // POST: api/compatibility/report
    [HttpPost("report")]
    public async Task<IActionResult> ReportCompatibilityIssue([FromBody] CompatibilityReportDto reportDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verificar que los paquetes existen
            var sourcePackage = await _context.Packages
                .FirstOrDefaultAsync(p => p.Id == reportDto.SourcePackageId ||
                                          p.Name == reportDto.SourcePackageName);

            var targetPackage = await _context.Packages
                .FirstOrDefaultAsync(p => p.Id == reportDto.TargetPackageId ||
                                          p.Name == reportDto.TargetPackageName);

            if (sourcePackage == null || targetPackage == null)
            {
                return BadRequest(new { message = "Uno o ambos paquetes no existen" });
            }

            // Crear reporte (en una tabla separada o como compatibilidad con baja confianza)
            var report = new
            {
                ReportId = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                SourcePackage = sourcePackage.Name,
                TargetPackage = targetPackage.Name,
                SourceVersion = reportDto.SourceVersion,
                TargetVersion = reportDto.TargetVersion,
                IssueType = reportDto.IssueType,
                Description = reportDto.Description,
                StepsToReproduce = reportDto.StepsToReproduce,
                ExpectedBehavior = reportDto.ExpectedBehavior,
                ActualBehavior = reportDto.ActualBehavior,
                Environment = reportDto.Environment,
                ReportedBy = reportDto.ReportedBy,
                Status = "pending_review",
                Priority = reportDto.Priority ?? "medium"
            };

            // Aquí normalmente guardarías en una tabla de reports
            _logger.LogInformation($"Reporte de compatibilidad recibido: {report}");

            // También podrías crear una compatibilidad con baja confianza
            var newCompatibility = new Compatibility
            {
                SourcePackageVersionId = await GetOrCreateVersionId(sourcePackage.Id, reportDto.SourceVersion),
                TargetPackageVersionId = await GetOrCreateVersionId(targetPackage.Id, reportDto.TargetVersion),
                TargetVersionConstraint = reportDto.TargetVersion,
                CompatibilityType = MapIssueToCompatibilityType(reportDto.IssueType),
                CompatibilityScore = 0, // Pendiente de verificación
                ConfidenceLevel = "user_reported",
                DetectedBy = "user_report",
                DetectionDate = DateTime.UtcNow,
                Notes = $"Reporte de usuario: {reportDto.Description}",
                IsActive = false // Inactivo hasta verificación
            };

            await _context.Compatibilities.AddAsync(newCompatibility);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Reporte enviado exitosamente",
                reportId = report.ReportId,
                compatibilityId = newCompatibility.Id,
                reviewNote = "El reporte será revisado por nuestro equipo"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando reporte de compatibilidad");
            return StatusCode(500, new { message = "Error interno", detail = ex.Message });
        }
    }

    #region Métodos Auxiliares

    private async Task<object> GetCompatibilityBetweenPackages(
        string sourceIdentifier,
        string targetIdentifier,
        string sourceVersion = null,
        string targetConstraint = null,
        bool isId = false)
    {
        // Buscar paquetes por ID o nombre
        Package sourcePackage, targetPackage;

        if (isId && int.TryParse(sourceIdentifier, out int sourceId))
        {
            sourcePackage = await _context.Packages.FindAsync(sourceId);
        }
        else
        {
            sourcePackage = await _context.Packages
                .FirstOrDefaultAsync(p => p.Name == sourceIdentifier || p.ShortName == sourceIdentifier);
        }

        if (isId && int.TryParse(targetIdentifier, out int targetId))
        {
            targetPackage = await _context.Packages.FindAsync(targetId);
        }
        else
        {
            targetPackage = await _context.Packages
                .FirstOrDefaultAsync(p => p.Name == targetIdentifier || p.ShortName == targetIdentifier);
        }

        if (sourcePackage == null || targetPackage == null)
        {
            return null;
        }

        // Buscar compatibilidades
        var query = _context.Compatibilities
            .Include(c => c.SourcePackageVersion)
            .Include(c => c.TargetPackageVersion)
            .Where(c => c.SourcePackageVersion.PackageId == sourcePackage.Id &&
                        c.TargetPackageVersion.PackageId == targetPackage.Id);

        if (!string.IsNullOrEmpty(sourceVersion))
        {
            query = query.Where(c => c.SourcePackageVersion.Version == sourceVersion);
        }

        if (!string.IsNullOrEmpty(targetConstraint))
        {
            query = query.Where(c => c.TargetVersionConstraint == targetConstraint);
        }

        var compatibilities = await query
            .OrderByDescending(c => c.CompatibilityScore)
            .Take(5)
            .Select(c => new
            {
                c.Id,
                sourceVersion = c.SourcePackageVersion.Version,
                targetVersion = c.TargetPackageVersion.Version,
                c.TargetVersionConstraint,
                c.CompatibilityType,
                c.CompatibilityScore,
                c.ConfidenceLevel,
                c.DetectedBy,
                c.DetectionDate,
                c.IsActive
            })
            .ToListAsync();

        // Generar recomendaciones
        var recommendations = compatibilities.Any()
            ? (IEnumerable)GenerateCompatibilityRecommendationsFromList(compatibilities.Cast<object>().ToList())
            : new[] { "No hay datos de compatibilidad disponibles" };

        return new
        {
            sourcePackage = new { sourcePackage.Id, sourcePackage.Name },
            targetPackage = new { targetPackage.Id, targetPackage.Name },
            compatibilitiesFound = compatibilities.Count,
            highestScore = compatibilities.Any() ? compatibilities.Max(c => c.CompatibilityScore) : 0,
            recommendations = recommendations,
            details = compatibilities
        };
    }

    private List<string> GenerateCompatibilityRecommendationsFromList(List<object> compatibilities)
    {
        var recommendations = new List<string>();

        if (compatibilities == null || !compatibilities.Any())
        {
            recommendations.Add("No hay datos de compatibilidad registrados");
            return recommendations;
        }

        // Usar reflexión para acceder a las propiedades
        int maxScore = 0;
        bool hasIncompatible = false;

        foreach (var comp in compatibilities)
        {
            var type = comp.GetType();
            
            // Obtener CompatibilityScore
            var scoreProp = type.GetProperty("CompatibilityScore");
            if (scoreProp != null)
            {
                var scoreValue = scoreProp.GetValue(comp);
                if (scoreValue is int score && score > maxScore)
                {
                    maxScore = score;
                }
            }

            // Verificar si es incompatible
            var typeProp = type.GetProperty("CompatibilityType");
            if (typeProp != null)
            {
                var typeValue = typeProp.GetValue(comp);
                if (typeValue is string compatibilityType && compatibilityType == "incompatible")
                {
                    hasIncompatible = true;
                }
            }
        }

        if (maxScore >= 90)
        {
            recommendations.Add($"Compatibilidad excelente ({maxScore}%)");
        }
        else if (maxScore >= 70)
        {
            recommendations.Add($"Compatibilidad moderada ({maxScore}%) - Verificar posibles issues");
        }
        else if (maxScore > 0)
        {
            recommendations.Add($"Compatibilidad baja ({maxScore}%) - Considerar alternativas");
        }

        if (hasIncompatible)
        {
            recommendations.Add("Se han detectado incompatibilidades conocidas");
        }

        return recommendations;
    }

    private List<string> GenerateRecommendations(List<object> analysisResults)
    {
        var recommendations = new List<string>();

        var deprecated = analysisResults.Where(r =>
            (bool)r.GetType().GetProperty("isDeprecated").GetValue(r)).ToList();

        if (deprecated.Any())
        {
            recommendations.Add($"Actualizar {deprecated.Count} paquete(s) obsoleto(s)");
        }

        var incompatible = analysisResults.Where(r =>
            ((List<object>)r.GetType().GetProperty("incompatibilities").GetValue(r)).Count > 0).ToList();

        if (incompatible.Any())
        {
            recommendations.Add($"Revisar {incompatible.Count} paquete(s) con incompatibilidades conocidas");
        }

        if (!recommendations.Any())
        {
            recommendations.Add("Todas las dependencias parecen compatibles");
        }

        return recommendations;
    }

    private async Task<int> GetOrCreateVersionId(int packageId, string version)
    {
        if (string.IsNullOrEmpty(version))
        {
            // Usar última versión
            var latest = await _context.PackageVersions
                .Where(v => v.PackageId == packageId && !v.IsDeprecated)
                .OrderByDescending(v => v.ReleaseDate)
                .FirstOrDefaultAsync();

            return latest?.Id ?? 0;
        }

        var existing = await _context.PackageVersions
            .FirstOrDefaultAsync(v => v.PackageId == packageId && v.Version == version);

        if (existing != null)
        {
            return existing.Id;
        }

        // Crear nueva versión (simplificado)
        var newVersion = new PackageVersion
        {
            PackageId = packageId,
            Version = version,
            ReleaseDate = DateTime.UtcNow,
            IsDeprecated = false,
            CreateAt = DateTime.UtcNow
        };

        await _context.PackageVersions.AddAsync(newVersion);
        await _context.SaveChangesAsync();

        return newVersion.Id;
    }

    private string MapIssueToCompatibilityType(string issueType)
    {
        return issueType?.ToLower() switch
        {
            "crash" or "error" => "incompatible",
            "warning" or "deprecation" => "partial",
            "performance" => "performance_issue",
            _ => "unknown"
        };
    }

    #endregion
}