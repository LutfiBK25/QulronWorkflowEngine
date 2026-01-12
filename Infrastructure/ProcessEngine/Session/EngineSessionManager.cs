
using Domain.ProcessEngine.Entities;
using Infrastructure.ProcessEngine.Execution;
using System.Collections.Concurrent;

namespace Infrastructure.ProcessEngine.Session;

/// <summary>
/// Manages all active device sessions in memory
/// Handles device registration, session lifecycle, and process execution
/// </summary>
public class EngineSessionManager
{
    private readonly ConcurrentDictionary<string, DeviceSession> _deviceSessions;
    private readonly ConcurrentDictionary<Guid, ExecutionSession> _executionSessions;
    private readonly ConcurrentDictionary<string, ActionResult> _deviceExecutionResults; // Stores last execution result per device
    private readonly TimeSpan _sessionTimeout = TimeSpan.FromHours(8);

    public EngineSessionManager()
    {
        _deviceSessions = new ConcurrentDictionary<string, DeviceSession>();
        _executionSessions = new ConcurrentDictionary<Guid, ExecutionSession>();
        _deviceExecutionResults = new ConcurrentDictionary<string, ActionResult>();

        // Start background cleanup task
        Task.Run(CleanupInactiveSessionsAsync);
    }

    // Device Session Management
    public DeviceSession RegisterDevice(string deviceId, Guid rootProcessModuleId)
    {
        var sessionId = Guid.NewGuid();
        var deviceSession = new DeviceSession
        {
            DeviceId = deviceId,
            SessionId = sessionId,
            ConnectedAt = DateTime.UtcNow,
            LastActivity = DateTime.UtcNow,
            Status = "CONNECTED",
            RootProcessModuleId = rootProcessModuleId,
            CurrentStep = 1
        };

        _deviceSessions[deviceId] = deviceSession;

        // Create execution session
        var executionSession = new ExecutionSession(userId: null, deviceId: deviceId);
        _executionSessions[sessionId] = executionSession;

        return deviceSession;
    }

    /// <summary>
    /// Starts the root process module for the specified device asynchronously using the provided execution engine.
    /// </summary>
    /// <remarks>If the process is paused and awaiting user input, the device session is updated to reflect
    /// the paused state. The method also updates the device session status and stores the execution result for later
    /// retrieval.</remarks>
    /// <param name="deviceId">The unique identifier of the device for which to start the process.</param>
    /// <param name="engine">The execution engine used to run the device's process module.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an ActionResult indicating the
    /// outcome of the process start operation. Returns a failure result if the device or execution session is not
    /// found, or if an error occurs during execution.</returns>
    public async Task<ActionResult> StartDeviceProcessAsync(string deviceId, ExecutionEngine engine)
    {
        var deviceSession = GetDeviceSession(deviceId);
        if (deviceSession == null)
        {
            return ActionResult.Fail($"Device {deviceId} not found");
        }

        var executionSession = GetExecutionSessionByDevice(deviceId);
        if (executionSession == null)
        {
            return ActionResult.Fail($"Execution session not found for device {deviceId}");
        }

        try
        {
            // Execute the root process module
            var result = await engine.ExecuteProcessModuleAsync(deviceSession.RootProcessModuleId, executionSession);

            // If paused (waiting for input), store screen JSON
            if (executionSession.IsPaused)
            {
                deviceSession.CurrentScreenJson = executionSession.PausedScreenJson;
                deviceSession.Status = "IDLE";  // Waiting for user input
            }
            else if (result.Result == Domain.ProcessEngine.Enums.ExecutionResult.Pass)
            {
                deviceSession.Status = "CONNECTED";
                deviceSession.CurrentScreenJson = "Process completed successfully";
            }
            else
            {
                deviceSession.Status = "CONNECTED";
                deviceSession.CurrentScreenJson = $"Process failed: {result.Message}";
            }

            // Store the result for later retrieval
            _deviceExecutionResults[deviceId] = result;

            return result;

        }
        catch (Exception ex)
        {
            return ActionResult.Fail($"Error starting device process: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Resume a paused device process with user input
    /// </summary>
    public async Task<ActionResult> ResumeDeviceProcessAsync(
        string deviceId,
        string userInput,
        ExecutionEngine engine)
    {
        var executionSession = GetExecutionSessionByDevice(deviceId);
        if (executionSession == null)
        {
            return ActionResult.Fail($"Execution session not found for device {deviceId}");
        }

        if (!executionSession.IsPaused)
        {
            return ActionResult.Fail($"Device {deviceId} is not paused");
        }

        try
        {
            // Resume the execution with user input
            var result = await engine.ResumeProcessModuleAsync(executionSession, userInput);

            var deviceSession = GetDeviceSession(deviceId);
            if (deviceSession != null)
            {
                // Update device session status based on execution state
                if (executionSession.IsPaused)
                {
                    deviceSession.Status = "IDLE";  // Still waiting for input
                    deviceSession.CurrentScreenJson = executionSession.PausedScreenJson;
                }
                else if (result.Result == Domain.ProcessEngine.Enums.ExecutionResult.Pass)
                {
                    deviceSession.Status = "CONNECTED";
                    deviceSession.CurrentScreenJson = "Process completed successfully";
                }
                else
                {
                    deviceSession.Status = "CONNECTED";
                    deviceSession.CurrentScreenJson = $"Process failed: {result.Message}";
                }

                deviceSession.LastActivity = DateTime.UtcNow;
            }

            // Store the result
            _deviceExecutionResults[deviceId] = result;

            return result;
        }
        catch (Exception ex)
        {
            return ActionResult.Fail($"Error resuming device process: {ex.Message}", ex);
        }
    }


    public DeviceSession GetDeviceSession(string deviceId)
    {
        _deviceSessions.TryGetValue(deviceId, out var session);
        return session;
    }

    public ExecutionSession GetExecutionSession(Guid sessionId)
    {
        _executionSessions.TryGetValue(sessionId, out var session);
        return session;
    }

    public ExecutionSession GetExecutionSessionByDevice(string deviceId)
    {
        var deviceSession = GetDeviceSession(deviceId);
        if (deviceSession == null) return null;

        return GetExecutionSession(deviceSession.SessionId);
    }

    public ActionResult GetLastExecutionResult(string deviceId)
    {
        _deviceExecutionResults.TryGetValue(deviceId, out var result);
        return result;
    }

    public void UpdateDeviceActivity(string deviceId)
    {
        if(_deviceSessions.TryGetValue(deviceId,out var session))
        {
            session.LastActivity = DateTime.UtcNow;
        }
    }

    public void SetDeviceUser(string deviceId, string userId)
    {
        if(_deviceSessions.TryGetValue(deviceId, out var session))
        {
            session.CurrentUserId = userId;
            session.Status = "ACTIVE";
            session.LastActivity = DateTime.UtcNow;
        }

        // Update execution session
        var executionSession = GetExecutionSessionByDevice(deviceId);
        if(executionSession != null)
        {
            executionSession.UserId = userId;
        }
    }

    public void ClearDeviceUser(string deviceId)
    {
        if( _deviceSessions.TryGetValue(deviceId,out var session))
        {
            session.CurrentUserId = null;
            session.Status = "CONNECTED";
            session.LastActivity = DateTime.UtcNow;
        }

        // Clear from execution session
        var executionSession = GetExecutionSessionByDevice(deviceId);
        if (executionSession != null)
        {
            executionSession.UserId = null;
        }
    }

    public void UpdateDeviceScreen(string deviceId, int step, string screenJson)
    {
        if(_deviceSessions.TryGetValue((deviceId),out var session))
        {
            session.CurrentStep = step;
            session.CurrentScreenJson = screenJson;
            session.LastActivity = DateTime.UtcNow;
        }
    }

    public void DisconnectDevice(string deviceId)
    {
        if(_deviceSessions.TryGetValue(deviceId, out var session))
        {
            session.Status = "DISCONNECTED";
            session.LastActivity = DateTime.UtcNow;
        }
    }

    public bool RemoveDevice(string deviceId)
    {
        if(_deviceSessions.TryGetValue(deviceId, out var session))
        {
            _deviceSessions.TryRemove(deviceId, out _);
            _executionSessions.TryRemove(session.SessionId, out _);
            _deviceExecutionResults.TryRemove(deviceId, out _);  // remove execution result
            return true;
        }
        return false;
    }

    //Query Methods
    public IEnumerable<DeviceSession> GetAllDeviceSessions()
    {
        return _deviceSessions.Values.ToList(); 
    }

    public IEnumerable<DeviceSession> GetActiveDevices()
    {
        return _deviceSessions.Values
            .Where(s => s.Status == "ACTIVE" || s.Status == "CONNECTED" || s.Status == "IDLE")
            .ToList();
    }

    public int GetActiveDeviceCount()
    {
        return _deviceSessions.Values
            .Count(s => s.Status == "ACTIVE" || s.Status == "CONNECTED" || s.Status == "IDLE");
    }

    public IEnumerable<DeviceSession> GetDevicesByUser(string userId)
    {
        return _deviceSessions.Values
            .Where(s => s.CurrentUserId == userId)
            .ToList();
    }


    private async void CleanupInactiveSessionsAsync()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromMinutes(5)); // Run every 5 minutes

            var now = DateTime.UtcNow;
            var expiredSessions = _deviceSessions.Values
                .Where(s => now - s.LastActivity > _sessionTimeout)
                .ToList();

            foreach (var session in expiredSessions)
            {
                Console.WriteLine($"Cleaning up expired session for device {session.DeviceId}");
                RemoveDevice(session.DeviceId);
            }
        }
    }

    public void ClearAllSessions()
    {
        _deviceSessions.Clear();
        _executionSessions.Clear();
    }

    // Statistics

    public SessionStatistics GetStatistics()
    {
        return new SessionStatistics
        {
            TotalDevices = _deviceSessions.Count,
            ActiveDevices = GetActiveDeviceCount(),
            TotalExecutionSessions = _executionSessions.Count,
            DevicesByStatus = _deviceSessions.Values
                .GroupBy(s => s.Status)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }
}

public class SessionStatistics
{
    public int TotalDevices { get; set; }
    public int ActiveDevices { get; set; }
    public int TotalExecutionSessions { get; set; }
    public Dictionary<string, int> DevicesByStatus { get; set; }
}