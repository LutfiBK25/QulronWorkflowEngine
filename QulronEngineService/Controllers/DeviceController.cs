using Infrastructure.ProcessEngine;
using Infrastructure.ProcessEngine.Session;
using Microsoft.AspNetCore.Mvc;
using QulronEngineService.Models;

namespace QulronEngineService.Controllers;

[ApiController]
[Route("api/devices")]
public class DeviceController : ControllerBase
{
    private readonly ExecutionEngine _engine;
    private readonly EngineSessionManager _sessionManager;
    private readonly ILogger<DeviceController> _logger;

    public DeviceController(
        ExecutionEngine engine,
        EngineSessionManager sessionManager,
        ILogger<DeviceController> logger )
    {
        _engine = engine;
        _sessionManager = sessionManager;
        _logger = logger;
    }

    // Connect a device scanner and get the initial screen
    [HttpPost("connect")]
    public async Task<ActionResult<DeviceScreenResponse>> Connect([FromQuery] string deviceId)
    {
        try
        {
            _logger.LogInformation($"Device connecting: {deviceId}");

            if (string.IsNullOrEmpty(deviceId))
            {
                return BadRequest(new { error = "Device ID is required" });
            }

            // Get Device session(created at engine startu[)
            var deviceSession = _sessionManager.GetDeviceSession(deviceId);
            if (deviceSession == null)
            {
                _logger.LogWarning($"Device not found: {deviceId}");
                return NotFound(new { error = $"Device {deviceId} not registered" });
            }

            _logger.LogInformation($"Device found: {deviceId}, Status: {deviceSession.Status}");

            // Update activity
            _sessionManager.UpdateDeviceActivity(deviceId);

            // Parse screen JSON if stored
            object screenJson = null;
            if(!string.IsNullOrEmpty(deviceSession.CurrentScreenJson))
            {
                try
                {
                    screenJson = System.Text.Json.JsonDocument.Parse(deviceSession.CurrentScreenJson).RootElement;
                }
                catch
                {
                    screenJson = deviceSession.CurrentScreenJson;
                }
            }

            return Ok(new DeviceScreenResponse
            {
                SessionId = deviceSession.SessionId.ToString(),
                ScreenJson = screenJson,
                Status = deviceSession.Status,
                Message = "Connected successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error connecting device: {ex.Message}");
            return StatusCode(500, new {error = "Internal server error", message = ex.Message});
        }
    }

    // Send user input to resume a paused device process
    [HttpPost("{deviceId}/input")]
    public async Task<ActionResult<DeviceScreenResponse>> SendInput(
        string deviceId, 
        [FromBody] DeviceInputRequest req)
    {
        try
        {
            _logger.LogInformation($"Device sending input: {deviceId}, value: {req.InputValue}");

            if (string.IsNullOrEmpty(deviceId) || req == null || string.IsNullOrEmpty(req.InputValue))
            {
                return BadRequest(new { error = "Device ID and input value are required" });
            }

            // Get device session
            var deviceSession = _sessionManager.GetDeviceSession(deviceId);
            if (deviceSession == null)
            {
                return NotFound(new { error = $"Device {deviceId} not found" });
            }

            // Get execution session
            var executionSession = _sessionManager.GetExecutionSessionByDevice(deviceId);
            if (executionSession == null)
            {
                return BadRequest(new { error = "No active execution session for device" });
            }

            // Check if paused
            if(!executionSession.IsPaused)
            {
                _logger.LogWarning($"Device {deviceId} is not paused, cannot send input");
                return BadRequest(new { error = "Device is not paused, cannot send input" });
            }

            // Resume process with user input
            var result = await _sessionManager.ResumeDeviceProcessAsync(deviceId, req.InputValue, _engine);

            if (result.Result == Domain.ProcessEngine.Enums.ExecutionResult.Fail)
            {
                _logger.LogError($"Process failed for device {deviceId}: {result.Message}");
                return BadRequest(new { error = result.Message });
            }

            // Parse screen JSON if paused
            object screenJson = null;
            if(executionSession.IsPaused && !string.IsNullOrEmpty(executionSession.PausedScreenJson))
            {
                try
                {
                    screenJson = System.Text.Json.JsonDocument.Parse(deviceSession.CurrentScreenJson).RootElement;
                }
                catch
                {
                    screenJson = deviceSession.CurrentScreenJson;
                }
            }

            return Ok(new DeviceScreenResponse
            {
                SessionId = deviceSession.SessionId.ToString(),
                ScreenJson = screenJson,
                Status = executionSession.IsPaused ? "paused" : "completed",
                Message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error sending input: {ex.Message}");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    // Get Device status
    [HttpGet("{deviceId}/status")]
    public ActionResult<DeviceStatusResponse> GetStatus(string deviceId)
    {
        try
        {
            _logger.LogInformation($"Getting status for device: {deviceId}");

            if (string.IsNullOrEmpty(deviceId))
            {
                return BadRequest(new { error = "Device ID is required" });
            }

            var deviceSession = _sessionManager.GetDeviceSession(deviceId);
            if (deviceSession == null)
            {
                return NotFound(new { error = $"Device {deviceId} not found" });
            }

            var executionSession = _sessionManager.GetExecutionSessionByDevice(deviceId);

            object screenJson = null;
            if (!string.IsNullOrEmpty(deviceSession.CurrentScreenJson))
            {
                try
                {
                    screenJson = System.Text.Json.JsonDocument.Parse(deviceSession.CurrentScreenJson).RootElement;
                }
                catch
                {
                    screenJson = deviceSession.CurrentScreenJson;
                }
            }

            return Ok(new DeviceStatusResponse
            {
                DeviceId = deviceId,
                Status = deviceSession.Status,
                CurrentUserId = deviceSession.CurrentUserId,
                ConnectedAt = deviceSession.ConnectedAt,
                LastActivity = deviceSession.LastActivity,
                CurrentStep = deviceSession.CurrentStep,
                CurrentScreenJson = screenJson,
                IsPaused = executionSession?.IsPaused ?? false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting status: {ex.Message}");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    // Get all active devices
    [HttpGet("list")]
    public ActionResult GetAllDevices()
    {
        try
        {
            _logger.LogInformation("Listing all devices");

            var devices = _sessionManager.GetAllDeviceSessions();
            var deviceList = devices.Select (d => new
            {
                d.DeviceId,
                d.Status,
                d.CurrentUserId,
                d.ConnectedAt,
                d.LastActivity,
                SessionId = d.SessionId.ToString(),
            }).ToList();

            return Ok(new { count = deviceList.Count , devices = deviceList});
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error listing devices: {ex.Message}");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }
        
}
