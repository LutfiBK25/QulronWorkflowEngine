using Domain.ProcessEngine.Entities;

namespace Infrastructure.ProcessEngine.Execution;

public class ModuleCache
{
    public Dictionary<Guid, Module> Modules { get; }
    public Dictionary<Guid, ProcessModule> ProcessModules { get; }
    public Dictionary<Guid, DatabaseActionModule> DatabaseActions { get; }
    public Dictionary<Guid, FieldModule> FieldModules { get; }

    public Dictionary<Guid, CompareActionModule> CompareActions { get; }
    public Dictionary<Guid, CalculateActionModule> CalculateActions { get; }
    public Dictionary<Guid, DialogActionModule> DialogActions { get; }
    public Dictionary<Guid, ScreenFormatModule> ScreenFormats { get; }


    // Quick lookup: field name (case-insensitive) -> field module
    public Dictionary<string, FieldModule> FieldsByName { get; }


    public ModuleCache()
    {
        Modules = new Dictionary<Guid, Module>();
        ProcessModules = new Dictionary<Guid, ProcessModule>();
        DatabaseActions = new Dictionary<Guid, DatabaseActionModule>();
        FieldModules = new Dictionary<Guid, FieldModule>();
        CompareActions = new Dictionary<Guid, CompareActionModule>();
        CalculateActions = new Dictionary<Guid, CalculateActionModule>();
        DialogActions = new Dictionary<Guid, DialogActionModule>();
        ScreenFormats = new Dictionary<Guid, ScreenFormatModule>();

        FieldsByName = new Dictionary<string, FieldModule>(StringComparer.OrdinalIgnoreCase);

    }

    public void AddModule(Module module)
    {
        Modules[module.Id] = module;
    }

    public void AddProcessModule(ProcessModule processModule)
    {
        ProcessModules[processModule.ModuleId] = processModule;
    }

    public void AddDatabaseAction(DatabaseActionModule dbAction)
    {
        DatabaseActions[dbAction.ModuleId] = dbAction;
    }

    public void AddFieldModule(FieldModule fieldModule)
    {
        FieldModules[fieldModule.ModuleId] = fieldModule;

        // Index by name for quick parameter mapping
        if (fieldModule.Module != null && !string.IsNullOrEmpty(fieldModule.Module.Name))
        {
            FieldsByName[fieldModule.Module.Name] = fieldModule;
        }
    }

    public void AddCompareAction(CompareActionModule compareAction)
    {
        CompareActions[compareAction.ModuleId] = compareAction;
    }

    public void AddCalculateAction(CalculateActionModule calculateAction)
    {
        CalculateActions[calculateAction.ModuleId] = calculateAction;
    }

    public void AddDialogAction(DialogActionModule dialogAction)
    {
        DialogActions[dialogAction.ModuleId] = dialogAction;
    }

    public void AddScreenFormat(ScreenFormatModule screenFormatModule)
    {
        ScreenFormats[screenFormatModule.ModuleId] = screenFormatModule;
    }

    public ProcessModule GetProcessModule(Guid moduleId)
    {
        return ProcessModules.TryGetValue(moduleId, out var pm) ? pm : null;
    }

    public DatabaseActionModule GetDatabaseAction(Guid moduleId)
    {
        return DatabaseActions.TryGetValue(moduleId, out var da) ? da : null;
    }

    public FieldModule GetFieldModule(Guid moduleId)
    {
        return FieldModules.TryGetValue(moduleId, out var fm) ? fm : null;
    }

    public FieldModule GetFieldModuleByName(string fieldName)
    {
        return FieldsByName.TryGetValue(fieldName, out var fm) ? fm : null;
    }

    public CompareActionModule GetCompareActionModule(Guid moduleId)
    {
        return CompareActions.TryGetValue(moduleId, out var ca) ? ca : null;
    }

    public CalculateActionModule GetCalculateActionModule(Guid moduleId)
    {
        return CalculateActions.TryGetValue(moduleId, out var ca) ? ca : null;
    }

    public DialogActionModule GetDialogActionModule(Guid moduleId)
    {
        return DialogActions.TryGetValue(moduleId, out var da) ? da : null;
    }

    public ScreenFormatModule GetScreenFormatModule(Guid moduleId)
    {
        return ScreenFormats.TryGetValue(moduleId, out var sf) ? sf : null;
    }
}
