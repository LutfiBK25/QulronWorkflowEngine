

namespace Infrastructure.ProcessEngine.Execution;

public class ExecutionFrame
{
    public Guid ProcessModuleId { get; set; }
    public string ProcessModuleName { get; set; }
    public int CurrentSequence { get; set; }
    public DateTime EnteredAt { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}
