namespace QulronEngineService.Models;
/// <summary>
/// Response with complete device session status
/// </summary>
public class DeviceStatusResponse
{
    public string DeviceId { get; set; }
    public string Status { get; set; }
    public string CurrentUserId { get; set; }
    public DateTime ConnectedAt { get; set; }
    public DateTime LastActivity {  get; set; }
    public int CurrentStep { get; set; }
    public object CurrentScreenJson { get; set; }
    public bool IsPaused { get; set; }
}
