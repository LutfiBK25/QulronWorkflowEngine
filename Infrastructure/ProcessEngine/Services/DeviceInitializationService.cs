
using Domain.ProcessEngine.Entities;
using Domain.ProcessEngine.Enums;
using Infrastructure.Presistence;
using Infrastructure.ProcessEngine.Execution;
using Infrastructure.ProcessEngine.Session;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.ProcessEngine.Services;

/// <summary>
/// Loads devices from database and initializes thier sessions
/// Called at engine startup
/// </summary>
public class DeviceInitializationService
{
    private readonly RepositoryDBContext _repositoryDBContext;
    private readonly ExecutionEngine _engine;
    private readonly EngineSessionManager _sessionManager;

    public DeviceInitializationService(
        RepositoryDBContext dbContext,
        ExecutionEngine engine,
        EngineSessionManager sessionManager 
        )
    {
        this._repositoryDBContext = dbContext;
        this._engine = engine;
        this._sessionManager = sessionManager;
    }

    /// <summary>
    /// Initializes sessions for all active devices by registering them and starting their default processes
    /// asynchronously. 
    /// </summary>
    /// <remarks>If no active devices are found in the database, the method completes without performing any
    /// initialization. For each device, a session is created and the default process is started; devices that require
    /// input are counted as successfully initialized. Device initialization failures are logged, but do not prevent the
    /// method from continuing with other devices. This method writes status and progress information to the console
    /// during execution.</remarks>
    /// <returns>A task that represents the asynchronous initialization operation. The task completes when all active devices
    /// have been processed.</returns>
    public async Task InitializeAllDevicesAsync()
    {
        try
        {
            Console.WriteLine("\n═══════════════════════════════════════════════════════");
            Console.WriteLine("  INITIALIZING DEVICE SESSIONS");
            Console.WriteLine("═══════════════════════════════════════════════════════");


            // Get all active devices from database
            var activeDevices = await _repositoryDBContext.Devices
                .Where(d => d.IsActive)
                .ToListAsync();

            if(activeDevices == null || activeDevices.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("* No active devices found in database");
                Console.ResetColor();
                Console.WriteLine();
                return;
            }

            Console.WriteLine($"Found {activeDevices.Count} active device(s)");
            Console.WriteLine();

            int successCount = 0;
            int failCount = 0;

            foreach(var device in activeDevices)
            {
                try
                {
                    Console.WriteLine($"Starting devices: {device.DeviceId} ({device.DeviceName})");

                    // Register device and create session
                    var deviceSession = _sessionManager.RegisterDevice(device.DeviceId, device.RootProcessModuleId);

                    Console.WriteLine($"  ├─ Session created: {deviceSession.SessionId}");
                    Console.WriteLine($"  ├─ Root process: {device.RootProcessModuleId}");


                    // Auto-start the default process (login process)
                    Console.WriteLine("  ├─ Starting default process...");

                    var result = await _sessionManager.StartDeviceProcessAsync(
                    device.DeviceId,
                    _engine
                    );

                    if (result.Result == ExecutionResult.Pass)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("  └─ ✓ Process started successfully");
                        Console.ResetColor();
                        successCount++;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"  └─ ⚠ Process paused (waiting for input): {result.Message}");
                        Console.ResetColor();
                        successCount++;  // Still counts as successful (it's paused waiting for input)
                    }

                }
                catch(Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  └─ ✗ Failed to initialize: {ex.Message}");
                    Console.ResetColor();
                    failCount++;
                }
                Console.WriteLine();
            }
            Console.WriteLine("───────────────────────────────────────────────────────");
            Console.WriteLine($"Device initialization complete: {successCount} success, {failCount} failed");

            var stats = _sessionManager.GetStatistics();
            Console.WriteLine($"Active sessions: {stats.TotalDevices}");
            Console.WriteLine("═══════════════════════════════════════════════════════\n");
        }
        catch(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Fatal error during device initialization: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Retrieves detailed information about a device, including its current session, execution session, and the last
    /// execution result.   
    /// </summary>
    /// <param name="deviceId">The unique identifier of the device for which to retrieve information. Cannot be null.</param>
    /// <returns>A <see cref="DeviceWithSessionInfo"/> object containing session and execution details for the specified device;
    /// or null if the device session does not exist.</returns>
    public DeviceWithSessionInfo GetDeviceInfo(string deviceId)
    {
        var deviceSession = _sessionManager.GetDeviceSession(deviceId);
        if (deviceSession == null) return null;

        var executionSession = _sessionManager.GetExecutionSessionByDevice(deviceId);
        var lastResult = _sessionManager.GetLastExecutionResult(deviceId);

        return new DeviceWithSessionInfo
        {
            DeviceId = deviceId,
            DeviceSession = deviceSession,
            ExecutionSession = executionSession,
            LastExecutionResult = lastResult
        };
    }

    /// <summary>
    /// Retrieves a collection of devices that have active sessions, including their associated session information.    
    /// </summary>
    /// <returns>An enumerable collection of <see cref="DeviceWithSessionInfo"/> objects representing devices with active
    /// sessions. The collection is empty if no devices are currently active.</returns>
    public IEnumerable<DeviceWithSessionInfo> GetAllDevicesInfo()
    {
        var devices = _sessionManager.GetAllDeviceSessions();
        return devices.Select(d => GetDeviceInfo(d.DeviceId)).Where(d => d != null);
    }
}


/// <summary>
/// Represents a device along with its associated session and execution information.
/// </summary>
public class DeviceWithSessionInfo
{
    public string DeviceId { get; set; }
    public DeviceSession DeviceSession { get; set; }
    public ExecutionSession ExecutionSession { get; set; }
    public ActionResult LastExecutionResult { get; set; }
}
