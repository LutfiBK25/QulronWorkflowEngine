using Domain.ProcessEngine.Enums;

namespace Domain.ProcessEngine.Entities;

public class Module
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public ModuleType ModuleType { get; set; }
    public int Version { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? LockedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }

    public Application Application { get; set; }
}
