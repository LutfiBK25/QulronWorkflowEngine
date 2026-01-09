

namespace Domain.ProcessEngine.Entities;

public class ScreenFormatModule
{
    public Guid ModuleId { get; set; }
    public int ScreenGroup {  get; set; } // 4=8x16, 6=6x40, 8=8x20
    public Guid? SoftKeyId { get; set; }

    public Module Module { get; set; }
    public SoftKeyModule SoftKey { get; set; }
    public List<ScreenFormatDetail> Details { get; set; } = new();
}

public class ScreenFormatDetail
{
    public Guid Id { get; set; }
    public Guid ScreenFormatId { get; set; }
    public int Sequence { get; set; }
    public int DataUsage { get; set; }
    public int DataType { get; set; } //Field Module Tyoe
    public Guid? DataId { get; set; }
    public int DataReference { get; set; }
    public string FormatId { get; set; }
    public int PosRow { get; set; }
    public int PosColumn { get; set; }
    public int PosWidth { get; set; }
    public int PosHeight { get; set; }
    public int Echo { get; set; }
    public int OverflowMode { get; set; }
    public ScreenFormatModule ScreenFormat {get; set;}
    
}