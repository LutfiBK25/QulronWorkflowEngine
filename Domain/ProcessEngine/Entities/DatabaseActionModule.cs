namespace Domain.ProcessEngine.Entities;

public class DatabaseActionModule
{
    public Guid ModuleId { get; set; }
    public string Statement { get; set; }
    public Module Module { get; set; }
}
