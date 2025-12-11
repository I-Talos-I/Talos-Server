using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace Talos.Server.Controllers;
[ApiController]
[Route("api/cache")]
public class CacheTestController : ControllerBase
{
    private readonly IDistributedCache _cache;
    public CacheTestController(IDistributedCache cache  )
    {
        _cache = cache;
    }
    
    [HttpGet("test")]
    public async Task<IActionResult> TestCache()
    {
        await _cache.SetStringAsync("pruebita", "pruebaaaa");
        var value = await _cache.GetStringAsync("pruebita");
        return Ok(value);
    }
    
}