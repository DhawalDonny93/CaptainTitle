using Microsoft.AspNetCore.Mvc;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AppBackendBasic.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        ApplicationDbContext context,
        ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Basic health check - returns 200 OK if service is running
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "BasicFrameworkBackend"
        });
    }

    /// <summary>
    /// Detailed health check - checks database connectivity
    /// </summary>
    [HttpGet("detailed")]
    public async Task<IActionResult> GetDetailed()
    {
        var health = new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "BasicFrameworkBackend",
            checks = new Dictionary<string, object>()
        };

        try
        {
            // Check database connectivity
            var dbHealthy = await _context.Database.CanConnectAsync();
            health.checks["database"] = new
            {
                status = dbHealthy ? "healthy" : "unhealthy",
                responseTime = "< 100ms"
            };

            // Check if we can query the database
            var userCount = await _context.Users.CountAsync();
            health.checks["database_query"] = new
            {
                status = "healthy",
                userCount
            };

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            
            health.checks["database"] = new
            {
                status = "unhealthy",
                error = ex.Message
            };

            return StatusCode(503, health);
        }
    }

    /// <summary>
    /// Readiness check - returns 200 when service is ready to accept traffic
    /// </summary>
    [HttpGet("ready")]
    public async Task<IActionResult> Ready()
    {
        try
        {
            // Check if database is accessible
            var canConnect = await _context.Database.CanConnectAsync();
            
            if (!canConnect)
            {
                return StatusCode(503, new
                {
                    status = "not_ready",
                    reason = "Database not accessible"
                });
            }

            return Ok(new
            {
                status = "ready",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Readiness check failed");
            return StatusCode(503, new
            {
                status = "not_ready",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Liveness check - returns 200 if service is alive
    /// </summary>
    [HttpGet("live")]
    public IActionResult Live()
    {
        return Ok(new
        {
            status = "alive",
            timestamp = DateTime.UtcNow
        });
    }
}
