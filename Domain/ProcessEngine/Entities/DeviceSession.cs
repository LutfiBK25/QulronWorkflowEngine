

namespace Domain.ProcessEngine.Entities;
/// <summary>
/// Represents an active device session (scanner/workstation)
/// </summary>
public class DeviceSession
{
    public string DeviceId { get; set; }
    public Guid SessionId { get; set; }
    public string CurrentUserId { get; set; }
    public DateTime ConnectedAt { get; set; }
    public DateTime LastActivity {  get; set; }
    public string Status { get; set; } // CONNECTED, ACTIVE, IDLE, DISCONNECTED
    public Guid RootProcessModuleId { get; set; }
    public int CurrentStep { get; set; }
    public string CurrentScreenJson { get; set; }
    public Dictionary<string, object> SessionData { get; set; } = new Dictionary<string, object>();
}
