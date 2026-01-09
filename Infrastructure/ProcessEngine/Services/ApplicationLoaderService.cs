
using Domain.ProcessEngine.Entities;
using Domain.ProcessEngine.Enums;
using Infrastructure.Presistence;
using Infrastructure.ProcessEngine.Execution;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.ProcessEngine.Services;

public class ApplicationLoaderService
{
    private readonly RepositoryDBContext _context;

    public ApplicationLoaderService(RepositoryDBContext context)
    {
        _context = context; 
    }

    public async Task<ModuleCache> LoadApplicationAsync(Guid applicationId)
    {
        var cache = new ModuleCache();

        try
        {
            // Load Application
            var application = await _context.Applications
                .Include(a => a.Modules)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null)
            {
                throw new InvalidOperationException($"Application {applicationId} not found");
            }

            // Load all modules for this application
            var modules = await _context.Modules
                .Where(a => a.ApplicationId == applicationId)
                .ToListAsync();

            foreach (var module in modules)
            {
                cache.AddModule(module);
            }

            // Load Process Modules with thier details
            var processModuleIds = modules
                .Where(m => m.ModuleType == ModuleType.ProcessModule)
                .Select(m => m.Id)
                .ToList();

            var processModules = await _context.ProcessModules
                .Include(pm => pm.Details.OrderBy(d => d.Sequence))
                .Where(pm => processModuleIds.Contains(pm.ModuleId))
                .ToListAsync();

            foreach (var pm in processModules)
            {
                // Attach the module reference
                pm.Module = cache.Modules[pm.ModuleId];
                cache.AddProcessModule(pm);
            }

            // Load Database Action Modules
            var dbActionModuleIds = modules
                .Where(m => m.ModuleType == ModuleType.DatabaseAction)
                .Select(m => m.Id)
                .ToList();

            var dbActionModules = await _context.DatabaseActionModules
                .Where(dam => dbActionModuleIds.Contains(dam.ModuleId))
                .ToListAsync();

            foreach (var dam in dbActionModules)
            {
                // Attach the module reference
                dam.Module = cache.Modules[dam.ModuleId];
                cache.AddDatabaseAction(dam);
            }

            // Load Field Modules
            var fieldModuleIds = modules
                .Where(m => m.ModuleType == ModuleType.FieldModule)
                .Select(m => m.Id)
                .ToList();

            var fieldModules = await _context.FieldModules
                .Where(fm => fieldModuleIds.Contains(fm.ModuleId))
                .ToListAsync();

            foreach (var fm in fieldModules)
            {
                // Attach the module reference
                fm.Module = cache.Modules[fm.ModuleId];
                cache.AddFieldModule(fm);
            }

            return cache;
        }
        catch (Exception ex)
        {
            // Log the error
            throw new InvalidOperationException($"Failed to load application {applicationId}", ex);
        }
    }

    public async Task<ProcessModule> GetProcessModuleAsync(Guid moduleId)
    {
        return await _context.ProcessModules
            .Include(pm => pm.Module)
            .Include(pm => pm.Details.OrderBy(d => d.Sequence))
            .FirstOrDefaultAsync(pm => pm.ModuleId == moduleId);
    }

    public async Task<DatabaseActionModule> GetDatabaseActionModuleAsync(Guid moduleId)
    {
        return await _context.DatabaseActionModules
            .Include(dam => dam.Module)
            .FirstOrDefaultAsync(dam => dam.ModuleId == moduleId);
    }

    public async Task<FieldModule> GetFieldModuleAsync(Guid moduleId)
    {
        return await _context.FieldModules
            .Include(fm => fm.Module)
            .FirstOrDefaultAsync(fm => fm.ModuleId == moduleId);
    }
}
