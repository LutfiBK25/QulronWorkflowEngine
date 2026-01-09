using Domain.ProcessEngine.Entities;
using Infrastructure.ProcessEngine.Execution;
using Domain.ProcessEngine.Enums;
namespace Infrastructure.ProcessEngine.Executors;

public class ProcessExecutor
{
    private readonly DatabaseExecutor _databaseExecutor;
    private readonly ModuleCache _moduleCache;
    // to protect against  infinte nesting in process modules
    private const int MaxCallDepth = 20;

    public ProcessExecutor(Dictionary<string, string> connectionStrings, ModuleCache moduleCache)
    {
        _databaseExecutor = new DatabaseExecutor(connectionStrings, moduleCache);
        _moduleCache = moduleCache;
    }

    public async Task<ActionResult> ExecuteAsync(
        Guid processModuleId,
        ExecutionSession session,
        Dictionary<string, object> parameters = null)
    {
        // Check call depth
        if(session.CallDepth >= MaxCallDepth)
        {
            return ActionResult.Fail($"Max call depth ({MaxCallDepth}) exceeded");
        }

        // Get process module from cache
        var processModule = _moduleCache.GetProcessModule(processModuleId);
        if (processModule == null)
        {
            return ActionResult.Fail($"Process module {processModuleId} not found in cache");
        }

        // Get module for name
        if (!_moduleCache.Modules.TryGetValue(processModuleId, out var module))
        {
            return ActionResult.Fail($"Module {processModuleId} not found in cache");
        }

        // Push Execution Frame
        var frame = new ExecutionFrame
        {
            ProcessModuleId = processModuleId,
            ProcessModuleName = module.Name,
            CurrentSequence = 1,
            EnteredAt = DateTime.UtcNow,
            Parameters = parameters ?? new Dictionary<string, object>()
        };
        session.PushFrame(frame);

        try
        {
            // Map parameters to fields by name
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    var fieldModule = _moduleCache.GetFieldModuleByName(param.Key);
                    if (fieldModule != null)
                    {
                        // Convert parameter value to appropriate field type
                        var convertedValue = ConvertToFieldType(param.Value, fieldModule.FieldType);
                        session.SetFieldValue(fieldModule.ModuleId, convertedValue);
                    }
                    else
                    {
                        // Parameter doesn't match any field - log warning but continue
                        // Could add logging here if needed
                    }
                }
            }
            
            // Execute steps
            var result = await ExecuteStepsAsync(processModule, session);

            return result;
        }
        finally
        {
            session.PopFrame();

            // Close connection when returning to top level (no more frames)
            if (session.CallDepth == 0)
            {
                await session.CloseConnectionAsync();
            }
        }
    }

    private object ConvertToFieldType(object value, FieldType fieldType)
    {
        if (value == null) return null;

        try
        {
            return fieldType switch
            {
                FieldType.String => value.ToString(),
                FieldType.Integer => Convert.ToInt32(value),
                FieldType.Boolean => Convert.ToBoolean(value),
                FieldType.DateTime => value is DateTime dt ? dt : DateTime.Parse(value.ToString()),
                _ => value
            };
        }
        catch
        {
            // If conversion fails, return original value
            return value;
        }
    }

    private async Task<ActionResult> ExecuteStepsAsync(
       ProcessModule processModule,
       ExecutionSession session)
    {
        var steps = processModule.Details.OrderBy(d => d.Sequence).ToList();
        var currentSequence = 1;
        // Protection against infinite loops inside a process module
        var maxIterations = 10000;
        var iterations = 0;

        while (iterations++ < maxIterations)
        {
            var step = steps.FirstOrDefault(s => s.Sequence == currentSequence);
            if (step == null)
            {
                return ActionResult.Fail($"Step with sequence {currentSequence} not found");
            }
            // skip commented steps
            if (step.CommentedFlag)
            {
                currentSequence++;
                continue;
            }

            // Execute Action
            ActionResult result;

            switch (step.ActionType)
            {
                case ActionType.ReturnPass:
                    {
                        return ActionResult.Pass("Process Completed");
                    }
                case ActionType.ReturnFail:
                    {
                        return ActionResult.Fail("Process Failed");
                    }
                case ActionType.Call:
                    // Call another process module
                    if (!step.ActionId.HasValue)
                    {
                        return ActionResult.Fail("Call action missing action Id");
                    }
                    result = await ExecuteAsync(step.ActionId.Value, session);
                    break;
                case ActionType.DatabaseExecute:
                    // Execute Database action
                    if (!step.ActionId.HasValue)
                    {
                        return ActionResult.Fail("Database action missing ActionId");
                    }
                    var dbAction = _moduleCache.GetDatabaseAction(step.ActionId.Value);
                    if (dbAction == null)
                    {
                        return ActionResult.Fail($"Database action {step.ActionId} not found in cache");
                    }
                    result = await _databaseExecutor.ExecuteAsync(dbAction, session);
                    break;
                default:
                    return ActionResult.Fail($"Unknown action type: {step.ActionType}");
            }

            // Determine next step based on result
            var nextLabel = result.Result == ExecutionResult.Pass
                ? step.PassLabel
                : step.FailLabel;

            currentSequence = ResolveNextSequence(steps, currentSequence, nextLabel);

            if (currentSequence == -1)
            {
                return result; // End execution
            }
        }

        return ActionResult.Fail($"Maximum iteration limit ({maxIterations}) reached");
    }

    private int ResolveNextSequence(
        List<ProcessModuleDetail> steps,
        int currentSequence,
        string label)
    {
        if (string.IsNullOrEmpty(label))
            return currentSequence + 1;

        switch (label.ToUpper())
        {
            case "NEXT":
                return currentSequence + 1;

            case "PREV":
                return currentSequence - 1;

            default:
                // Jump to labeled step
                var targetStep = steps.FirstOrDefault(s =>
                    s.LabelName?.Equals(label, StringComparison.OrdinalIgnoreCase) == true);
                return targetStep?.Sequence ?? -1;
        }
    }
}
