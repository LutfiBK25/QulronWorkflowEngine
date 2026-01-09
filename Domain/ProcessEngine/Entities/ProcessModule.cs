
using Domain.ProcessEngine.Enums;

namespace Domain.ProcessEngine.Entities;

public class ProcessModule
{
    public Guid ModuleId { get; set; }
    public string Subtype { get; set; }
    public bool Remote { get; set; }
    public bool DynamicCall { get; set; }
    public string? Comment { get; set; }
    public Module Module { get; set; }
    public List<ProcessModuleDetail> Details { get; set; } = new();
}

public class ProcessModuleDetail
{
    public Guid Id { get; set; }
    public Guid ProcessModuleId { get; set; }
    public int Sequence {  get; set; }
    public string? LabelName { get; set; }
    public ActionType? ActionType { get; set; }
    public Guid? ActionId { get; set; }
    public ModuleType? ActionModuleType { get; set; }
    public string PassLabel {  get; set; }
    public string FailLabel { get; set; }
    public bool CommentedFlag { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedDate { get; set; }

    public ProcessModule ProcessModule { get; set; }
}
