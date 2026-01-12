namespace QulronEngineService.Models;
/// <summary>
/// Response with engine health and statistics
/// </summary>
public class EngineHealthResponse
{
    public string Status { get; set; }
    public DateTime StartTime {  get; set; }
    public TimeSpan UpTime { get; set; }
    public int ActiveDevices { get; set; }
    public int TotalSessions { get; set; }
    public int LoadedModules { get; set; }
    public int ProcessModules { get; set; }
    public int DatabaseActions { get; set; }
    public int FieldModules { get; set; }
    public Dictionary<string, int> DevicesByStatus { get; set; }
}
