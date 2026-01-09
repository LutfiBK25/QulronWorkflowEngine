

using Domain.ProcessEngine.Entities;
using Infrastructure.ProcessEngine.Execution;
using Infrastructure.ProcessEngine.Rendering;

namespace Infrastructure.ProcessEngine.Executors;

public class DialogExecutor
{
    private readonly ModuleCache _moduleCache;
    private readonly ScreenBuilder _screenBuilder;

    public DialogExecutor(ModuleCache moduleCache)
    {
        _moduleCache = moduleCache; 
        _screenBuilder = new ScreenBuilder(moduleCache);
    }

    public async Task<ActionResult> ExecuteAsync(
        DialogActionModule dialogAction,
        ExecutionSession session,
        int currentStep)
    {
        // Get appropriate screen format for current device ( based on screen group)
        // For now, just use first detail
        var detail = dialogAction.Details.FirstOrDefault();
        if(detail == null)
        {
            return ActionResult.Fail("No Screen format defined for dialog");
        }

        var screenFormat = _moduleCache.ScreenFormats.TryGetValue(detail.ScreenFormatId, out var sf) ? sf : null;

        if (screenFormat == null)
        {
            return ActionResult.Fail($"Screen format {detail.ScreenFormatId} not found");
        }

        // Build screen JSON
        var screenJson = _screenBuilder.BuildScreenJson(screenFormat, session);

        // Pause execution and wait for input
        session.Pause(dialogAction.ModuleId, currentStep, screenJson);

        return ActionResult.Pass($"Dialog displayed, wait for input");
    }

    public ActionResult ProcessInput(
        DialogActionModule dialogAction,
        ExecutionSession session,
        string inputValue)
    {
        //Find which field to populate from the screen format
        var detail = dialogAction.Details.FirstOrDefault();

        if (detail == null)
        {
            return ActionResult.Fail("No screen format detail");
        }

        var screenFormat = _moduleCache.ScreenFormats.TryGetValue(detail.ScreenFormatId, out var sf) ? sf : null;

        if (screenFormat == null)
        {
            return ActionResult.Fail("Screen format not found");
        }

        // Find the input field in screen format details;
        var inputDetail = screenFormat.Details.FirstOrDefault(d => d.DataUsage == 1);

        if (inputDetail?.DataId.HasValue == true)
        {
            // Store input in the field
            session.SetFieldValue(inputDetail.DataId.Value, inputValue);
        }

        // Resume execution
        session.Resume();

        return ActionResult.Pass("Input processed");

    }
}
