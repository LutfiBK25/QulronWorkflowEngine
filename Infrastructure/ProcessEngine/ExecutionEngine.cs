

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

        foreach (var dam in loadedCache.DatabaseActions.Values)
            _moduleCache.AddDatabaseAction(dam);

        foreach (var fm in loadedCache.FieldModules.Values)
            _moduleCache.AddFieldModule(fm);
    }

    public async Task<ActionResult> ExecuteProcessModuleAsync(
        Guid processModuleId,
        string userId = null,
        Dictionary<string, object> parameters = null)
    {
        var session = new ExecutionSession(userId);
        return await _processExecutor.ExecuteAsync(processModuleId, session, parameters);
    }
}
