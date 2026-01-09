
namespace Domain.ProcessEngine.Entities;
/// <summary>
/// Device registry (stored in Engine DB)
/// </summary>
public class Device
{
    public string DeviceId { get; set; }
    public string DeviceName { get; set; }
    public string DeviceType { get; set; } // SCANNER, WORKSTATION
    public Guid RootProcessModuleId { get; set; } // Log-On process
    public DateTime RegisteredAt { get; set; }
    public DateTime? LastConnected {  get; set; }
    public bool IsActive { get; set; }
    public string Metadata { get; set; }  // Json
}
