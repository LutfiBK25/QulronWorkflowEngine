
namespace Domain.ProcessEngine.Entities;

public class DialogActionModule
{
    public Guid ModuleId { get; set; }
    public Module Module { get; set; }
    public List<DialogActionDetail> Details { get; set; } = new();
}

public class DialogActionDetail
{
    public Guid Id { get; set; }
    public Guid DialogActionId { get; set; }
    public int ScreenGroup {  get; set; } // 4, 6, 8
    public Guid ScreenFormatId { get; set; }
    public int Reference {  get; set; }
    public bool KeyEntry { get; set; }

    public DialogActionModule DialogAction {  get; set; }
    public ScreenFormatModule ScreenFormat { get; set; }

}
