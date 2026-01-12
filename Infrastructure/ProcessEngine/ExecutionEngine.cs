

using Domain.ProcessEngine.Entities;
using Infrastructure.ProcessEngine.Execution;
using Infrastructure.ProcessEngine.Executors;
using Infrastructure.ProcessEngine.Services;

namespace Infrastructure.ProcessEngine;

public class ExecutionEngine
{
    private readonly Dictionary<string, string> _connectionStrings;
    private readonly ModuleCache _moduleCache;
    private readonly ProcessExecutor _processExecutor;
    private readonly ApplicationLoaderService _loaderService;


    public ExecutionEngine(
        Dictionary<string, string> connectionStrings,
        ApplicationLoaderService loaderService)
    {
        _connectionStrings = connectionStrings;
        _loaderService = loaderService;
        _moduleCache = new ModuleCache();
        _processExecutor = new ProcessExecutor(connectionStrings, _moduleCache);
    }

    public ModuleCache Cache => _moduleCache;


    public async Task LoadApplicationAsync(Guid applicationId)
    {
        // Call the loader service to populate the cache
        var loadedCache = await _loaderService.LoadApplicationAsync(applicationId);

        // Copy all data from loaded cache into our cache
        foreach (var module in loadedCache.Modules.Values)
            _moduleCache.AddModule(module);

        foreach (var pm in loadedCache.ProcessModules.Values)
            _moduleCache.AddProcessModule(pm);

        foreach (var ca in loadedCache.CompareActions.Values)
            _moduleCache.AddCompareAction(ca);

        foreach (var cal in loadedCache.CalculateActions.Values)
            _moduleCache.AddCalculateAction(cal);

        foreach (var dam in loadedCache.DatabaseActions.Values)
            _moduleCache.AddDatabaseAction(dam);

        foreach (var fm in loadedCache.FieldModules.Values)
            _moduleCache.AddFieldModule(fm);

        foreach (var da in loadedCache.DialogActions.Values)
            _moduleCache.AddDialogAction(da);

        foreach (var sf in loadedCache.ScreenFormats.Values)
            _moduleCache.AddScreenFormat(sf);
    }

    /// <summary>
    /// Execute a process module with parameters (creates new session)
    /// Used for console testing
    /// </summary>
    public async Task<ActionResult> ExecuteProcessModuleAsync(
        Guid processModuleId,
        string userId = null,
        Dictionary<string, object> parameters = null)
    {
        var session = new ExecutionSession(userId);
        return await _processExecutor.ExecuteAsync(processModuleId, session, parameters);
    }

    /// <summary>
    /// Execute a process module with an existing execution session
    /// Used for device auto-start (session already created by EngineSessionManager)
    /// </summary>
    public async Task<ActionResult> ExecuteProcessModuleAsync(
        Guid processModuleId,
        ExecutionSession session,
        Dictionary<string, object> parameters = null)
    {
        return await _processExecutor.ExecuteAsync(processModuleId, session, parameters);
    }

    /// <summary>
    /// Resume a paused process from user input
    /// Used when device sends user input to continue execution
    /// </summary>
    public async Task<ActionResult> ResumeProcessModuleAsync(
        ExecutionSession session,
        string inputValue)
    {
        return await _processExecutor.ResumeAfterDialogAsync(session, inputValue);
    }
}
