
namespace Domain.ProcessEngine.Entities;

public class SoftKeyModule
{
    public Guid ModuleId { get; set; }
    public int Layer {  get; set; }
    public int State { get; set; }

    public Module Module { get; set; }
    public List<SoftKeyDetail> Details { get; set; }
}

public class SoftKeyDetail
{
    public Guid Id { get; set; }
    public Guid SoftKeyId { get; set; }
    public int Sequence {  get; set; }
    public int OperatorId { get; set; }
    public int InputType { get; set; }
    public Guid? InputId { get; set; }
    public int OutputType { get; set; }
    public Guid? OutputId { get; set; }

    public SoftKeyModule SoftKey { get; set; }
