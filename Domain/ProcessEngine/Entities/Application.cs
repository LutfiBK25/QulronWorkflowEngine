namespace Domain.ProcessEngine.Entities;

public class Application
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public string VersionBuild { get; set; }
    public DateTime? LastCompiled {  get; set; }
    public DateTime? LastActivated { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }

    public List<Module> Modules { get; set; } = new();
}
