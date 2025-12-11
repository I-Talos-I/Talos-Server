using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Text.Json;
using Talos.Server.Models.Dtos;

namespace Talos.Server.Controllers
{
    [ApiController]
    [Route("api/install")]
    public class InstallController : ControllerBase
    {
        private readonly IDatabase _db;

        public InstallController(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        // 1. Pre-flight check
        [HttpPost("preflight")]
        public IActionResult Preflight([FromBody] PreflightRequest request)
        {
            var response = new PreflightResponse
            {
                IsValid = true,
                Warnings = Array.Empty<string>(),
                Errors = Array.Empty<string>()
            };

            // Simulación: si falta Docker, marcar error
            if (!request.RequiredComponents.Contains("Docker"))
            {
                response.IsValid = false;
                response.Errors = new[] { "Docker no está instalado." };
            }

            return Ok(response);
        }

        // 2. Generar script desde plantilla
        [HttpGet("template/{id}/script")]
        public IActionResult GetTemplateScript(int id)
        {
            var script = $"echo Instalando template {id}...\necho Instalación completa";
            return Ok(new { Script = script });
        }

        // 3. Registrar log
        [HttpPost("log")]
        public async Task<IActionResult> LogInstall([FromBody] InstallLogRequest log)
        {
            var key = $"install:{log.InstallId}";
            var raw = await _db.StringGetAsync(key);
            InstallStatusResponse status;

            if (raw.HasValue)
            {
                status = JsonSerializer.Deserialize<InstallStatusResponse>(raw)!;
            }
            else
            {
                return NotFound(new { Message = "Instalación no encontrada" });
            }

            status.Logs.Add($"{log.Timestamp}: [{log.Level}] {log.Message}");
            status.Progress += 10;
            if (status.Progress >= 100)
            {
                status.Progress = 100;
                status.Status = "Completed";
            }

            await _db.StringSetAsync(key, JsonSerializer.Serialize(status));
            return Ok(new { Message = "Log registrado" });
        }

        // 4. Obtener estado
        [HttpGet("status/{installId}")]
        public async Task<IActionResult> GetInstallStatus(string installId)
        {
            var key = $"install:{installId}";
            var raw = await _db.StringGetAsync(key);

            if (!raw.HasValue)
                return NotFound(new { Message = "Instalación no encontrada" });

            var status = JsonSerializer.Deserialize<InstallStatusResponse>(raw)!;
            return Ok(status);
        }

        // 5. Iniciar instalación (genera installId y guarda estado en Redis)
        [HttpPost("start")]
        public async Task<IActionResult> StartInstallation()
        {
            var installId = Guid.NewGuid().ToString();
            var status = new InstallStatusResponse
            {
                InstallId = installId,
                Status = "InProgress",
                Progress = 0,
                Errors = Array.Empty<string>(),
                Logs = new List<string>()
            };

            await _db.StringSetAsync($"install:{installId}", JsonSerializer.Serialize(status));
            return Ok(new { InstallId = installId });
        }
    }
}