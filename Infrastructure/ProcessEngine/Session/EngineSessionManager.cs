
using Domain.ProcessEngine.Entities;
using Infrastructure.ProcessEngine.Execution;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Infrastructure.ProcessEngine.Session;

/// <summary>
/// Manages all active device sessions in memory
/// </summary>
public class EngineSessionManager
{
    private readonly ConcurrentDictionary<string, DeviceSession> _deviceSessions;
    private readonly ConcurrentDictionary<Guid, ExecutionSession> _executionSessions;
    private readonly TimeSpan _sessionTimeout = TimeSpan.FromHours(8);

    public EngineSessionManager()
    {
        _deviceSessions = new ConcurrentDictionary<string, DeviceSession>();
        _executionSessions = new ConcurrentDictionary<Guid, ExecutionSession>();

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
            .Where(s => s.Status == "ACTIVE" || s.Status == "CONNECTED")
            .ToList();
    }

    public int GetActiveDeviceCount()
    {
        return _deviceSessions.Values
            .Count(s => s.Status == "ACTIVE" || s.Status == "CONNECTED");
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