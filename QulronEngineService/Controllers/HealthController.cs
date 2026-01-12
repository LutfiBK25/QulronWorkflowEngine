using Infrastructure.ProcessEngine;
using Infrastructure.ProcessEngine.Session;
using Microsoft.AspNetCore.Mvc;
using QulronEngineService.Models;

namespace QulronEngineService.Controllers;

[ApiController]
[Route("api")]
public class HealthController : ControllerBase
{
    private readonly ExecutionEngine _engine;
    private readonly EngineSessionManager _sessionManager;
    private readonly ILogger<HealthController> _logger;
    private static readonly DateTime StartTime = DateTime.UtcNow;

    public HealthController(
        ExecutionEngine engine,
        EngineSessionManager sessionManager,
        ILogger<HealthController> logger )
    {
        _engine = engine;
        _sessionManager = sessionManager;
        _logger = logger;
    }

    // Get engine health and statistics
    [HttpGet("health")]
    public ActionResult<EngineHealthResponse> GetHealth()
    {
        try
        {
            var stats = _sessionManager.GetStatistics();
            var uptime = DateTime.UtcNow - StartTime;

            return Ok(new EngineHealthResponse
            {
                Status = "Healthy",
                StartTime = StartTime,
                UpTime = uptime,
                ActiveDevices = stats.ActiveDevices,
                TotalSessions = stats.TotalExecutionSessions,
                LoadedModules = _engine.Cache.Modules.Count,
                ProcessModules = _engine.Cache.ProcessModules.Count,
                DatabaseActions = _engine.Cache.DatabaseActions.Count,
                FieldModules = _engine.Cache.FieldModules.Count,
                DevicesByStatus = stats.DevicesByStatus,
            });
        }
        catch(Exception ex)
        {
            _logger.LogError($"Error getting health: {ex.Message}");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }
}
