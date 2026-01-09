
using Domain.ProcessEngine.Enums;

namespace Domain.ProcessEngine.Entities;

// CALCULATE
public class CalculateActionModule
{
    public Guid ModuleId { get; set; }
    public List<CalculateModuleDetail> Details { get; set; } = new();

    public Module Module { get; set; }
}

public class CalculateModuleDetail
{
    public Guid Id { get; set; }
    public Guid CalculateActionModuleId { get; set; }
    public int Sequence { get; set; }

    public CalculateOperator OperatorId { get; set; }
    // Input 1
    public bool Input1IsConstant { get; set; }
    public Guid? Input1FieldId { get; set; }
    public string Input1Value { get; set; }

    // Input 2 (for binary operations)
    public bool Input2IsConstant { get; set; }
    public Guid? Input2FieldId { get; set; }
    public string Input2Value { get; set; }

    // Result
    public Guid ResultFieldId { get; set; }
    public CalculateActionModule CalculateActionModule { get; set; }

}