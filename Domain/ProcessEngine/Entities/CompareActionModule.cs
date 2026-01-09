

using Domain.ProcessEngine.Enums;

namespace Domain.ProcessEngine.Entities;
/// <summary>
/// Compare Field Modules
/// </summary>
public class CompareActionModule
{
    public Guid ModuleId { get; set; }
    public CompareOperator OperatorId { get; set; }

    // Input 1
    public bool Input1IsConstant { get; set; }
    public Guid? Input1FieldId { get; set; }
    public string Input1Value { get; set; }  // Constant OR field value at runtime

    // Input 2
    public bool Input2IsConstant { get; set; }
    public Guid? Input2FieldId { get; set; }
    public string Input2Value { get; set; }

    public Module Module { get; set; }
}