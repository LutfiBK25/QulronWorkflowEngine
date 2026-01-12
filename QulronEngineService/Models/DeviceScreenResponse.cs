namespace QulronEngineService.Models;

/// <summary>
/// Response woth screem JSON for device display
/// </summary>
public class DeviceScreenResponse
{
    public string SessionId { get; set; }
    public object ScreenJson { get; set; }
    public string Status { get; set; }
    public string Message { get; set; }
}
