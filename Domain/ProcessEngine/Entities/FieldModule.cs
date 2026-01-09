
using Domain.ProcessEngine.Enums;

namespace Domain.ProcessEngine.Entities;

public class FieldModule
{
    public Guid ModuleId { get; set; }
    public FieldType FieldType { get; set; }
    public string? DefaultValue { get; set; }
    public Module Module { get; set; }
}


