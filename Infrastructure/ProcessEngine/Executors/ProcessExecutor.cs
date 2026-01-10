using Domain.ProcessEngine.Entities;
using Infrastructure.ProcessEngine.Execution;
using Domain.ProcessEngine.Enums;
namespace Infrastructure.ProcessEngine.Executors;

public class ProcessExecutor
{
    private readonly CalculateExecutor _calculateExecutor;
    private readonly CompareExecutor _compareExecutor;
    private readonly DatabaseExecutor _databaseExecutor;
    private readonly DialogExecutor _dialogExecutor;

    private readonly ModuleCache _moduleCache;
    // to protect against infinite nesting in process modules
    private const int MaxCallDepth = 20;

    public ProcessExecutor(Dictionary<string, string> connectionStrings, ModuleCache moduleCache)
    {
        _calculateExecutor = new CalculateExecutor();
        _compareExecutor = new CompareExecutor();
        _databaseExecutor = new DatabaseExecutor(connectionStrings, moduleCache);
        _dialogExecutor = new DialogExecutor(moduleCache);
        _moduleCache = moduleCache;
    }

    public async Task<ActionResult> ExecuteAsync(
        Guid processModuleId,
        ExecutionSession session,
        Dictionary<string, object> parameters = null)
    {
        // Check call depth
        if (session.CallDepth >= MaxCallDepth)
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
                }
            }

            // Execute steps starting from sequence 1
            var result = await ExecuteStepsFromSequenceAsync(processModule, session, 1);

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

    /// <summary>
    /// Execute steps starting from a given sequence number
    /// Used by both ExecuteAsync (sequence 1) and ResumeAfterDialogAsync (next sequence)
    /// </summary>
    private async Task<ActionResult> ExecuteStepsFromSequenceAsync(
        ProcessModule processModule,
        ExecutionSession session,
        int startingSequence)
    {
        var steps = processModule.Details.OrderBy(d => d.Sequence).ToList();
        var currentSequence = startingSequence;
        var maxIterations = 10000;
        var iterations = 0;

        while (iterations++ < maxIterations)
        {
            var step = steps.FirstOrDefault(s => s.Sequence == currentSequence);
            if (step == null)
            {
                return ActionResult.Fail($"Step with sequence {currentSequence} not found");
            }

            // Skip commented steps
            if (step.CommentedFlag)
            {
                currentSequence++;
                continue;
            }

            // Execute the step and get result
            var result = await ExecuteStepAsync(step, session, currentSequence);

            // If dialog paused execution, return immediately
            if (session.IsPaused)
            {
                return result;
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

    /// <summary>
    /// Execute a single step action
    /// </summary>
    private async Task<ActionResult> ExecuteStepAsync(
        ProcessModuleDetail step,
        ExecutionSession session,
        int currentSequence)
    {
        switch (step.ActionType)
        {
            case ActionType.ReturnPass:
                return ActionResult.Pass("Process Completed");

            case ActionType.ReturnFail:
                return ActionResult.Fail("Process Failed");

            case ActionType.Call:
                if (!step.ActionId.HasValue)
                {
                    return ActionResult.Fail("Call action missing action Id");
                }
                return await ExecuteAsync(step.ActionId.Value, session);

            case ActionType.Compare:
                if (!step.ActionId.HasValue)
                {
                    return ActionResult.Fail("Compare action missing action Id");
                }
                var compareAction = _moduleCache.GetCompareActionModule(step.ActionId.Value);
                if (compareAction == null)
                {
                    return ActionResult.Fail($"Compare action {step.ActionId} not found in cache");
                }
                return await _compareExecutor.ExecuteAsync(compareAction, session);

            case ActionType.Calculate:
                if (!step.ActionId.HasValue)
                {
                    return ActionResult.Fail("Calculate action missing ActionId");
                }
                var calculateAction = _moduleCache.GetCalculateActionModule(step.ActionId.Value);
                if (calculateAction == null)
                {
                    return ActionResult.Fail($"Calculate action {step.ActionId} not found in cache");
                }
                return await _calculateExecutor.ExecuteAsync(calculateAction, session);

            case ActionType.DatabaseExecute:
                if (!step.ActionId.HasValue)
                {
                    return ActionResult.Fail("Database action missing ActionId");
                }
                var dbAction = _moduleCache.GetDatabaseAction(step.ActionId.Value);
                if (dbAction == null)
                {
                    return ActionResult.Fail($"Database action {step.ActionId} not found in cache");
                }
                return await _databaseExecutor.ExecuteAsync(dbAction, session);

            case ActionType.Dialog:
                if (!step.ActionId.HasValue)
                {
                    return ActionResult.Fail("Dialog action missing Action Id");
                }
                var dialogAction = _moduleCache.GetDialogActionModule(step.ActionId.Value);
                if (dialogAction == null)
                {
                    return ActionResult.Fail($"Dialog action {step.ActionId} not found in cache");
                }
                return await _dialogExecutor.ExecuteAsync(dialogAction, session, currentSequence);

            default:
                return ActionResult.Fail($"Unknown action type: {step.ActionType}");
        }
    }

    /// <summary>
    /// Determine where to go for the next step
    /// </summary>
    private int ResolveNextSequence(
        List<ProcessModuleDetail> steps,
        int currentSequence,
        string label)
    {
        if (string.IsNullOrEmpty(label))
            return currentSequence + 1;

        return label.ToUpper() switch
        {
            "NEXT" => currentSequence + 1,
            "PREV" => currentSequence - 1,
            _ => steps.FirstOrDefault(s =>
                    s.LabelName?.Equals(label, StringComparison.OrdinalIgnoreCase) == true)
                ?.Sequence ?? -1
        };
    }

    /// <summary>
    /// Resume execution after dialog input
    /// </summary>
    public async Task<ActionResult> ResumeAfterDialogAsync(
        ExecutionSession session,
        string inputValue)
    {
        if (!session.IsPaused)
        {
            return ActionResult.Fail("Session is not paused");
        }

        if (!session.CanResume())
        {
            return ActionResult.Fail("Session cannot be resumed - missing pause context");
        }

        // Get the dialog action that paused execution
        var dialogAction = _moduleCache.GetDialogActionModule(session.PausedAtProcessModuleId.Value);
        if (dialogAction == null)
        {
            return ActionResult.Fail($"Dialog action {session.PausedAtProcessModuleId} not found");
        }

        // Process the input
        var inputResult = _dialogExecutor.ProcessInput(dialogAction, session, inputValue);
        if (inputResult.Result == ExecutionResult.Fail)
        {
            return inputResult;
        }

        // Get the process module we were executing
        var currentFrame = session.CurrentFrame;
        if (currentFrame == null)
        {
            return ActionResult.Fail("No execution frame available");
        }

        var processModule = _moduleCache.GetProcessModule(currentFrame.ProcessModuleId);
        if (processModule == null)
        {
            return ActionResult.Fail($"Process module {currentFrame.ProcessModuleId} not found");
        }

        // Resume execution from next step
        var nextSequence = session.PausedAtStep.Value + 1;
        return await ExecuteStepsFromSequenceAsync(processModule, session, nextSequence);
    }
}
