using Domain.ProcessEngine.Enums;
using Infrastructure.Presistence;
using Infrastructure.ProcessEngine;
using Infrastructure.ProcessEngine.Execution;
using Infrastructure.ProcessEngine.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace QulronEngineConsoleApp;

internal class Program
{
    private static ExecutionEngine? _engine;
    private static IConfiguration? _configuration;
    static async Task Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     Process Engine - Console Test Application          ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        try
        {
            // Load Configuration from appsettings.json
            await LoadConfigurationAsync();

            // Initialize Engine
            await InitializeEngineAsync();

            // Load Applications
            await LoadApplicationsAsync();

            // Main Menu
            await ShowMainMenuAsync();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n* Fatal Error: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            Console.ResetColor();
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }



    private static async Task LoadConfigurationAsync()
    {
        Console.WriteLine("Loading configuration...");

        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json",optional: false, reloadOnChange: true)
            .Build();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("* Configuration loaded sucessfully");
        Console.ResetColor();
        Console.WriteLine();
    }

    private static async Task InitializeEngineAsync()
    {
        Console.WriteLine("Initializing Process Engine...");

        // Get connection strings
        var repositoryConnectionString = _configuration.GetConnectionString("RepositoryDB");
        var wmsConnectionString = _configuration.GetConnectionString("WMS");
        var engineConnectionString = _configuration.GetConnectionString("ENGINE");

        if (string.IsNullOrEmpty(repositoryConnectionString))
        {
            throw new InvalidOperationException("RepositoryDB connection string not found in configuration");
        }

        // Setup DbContext
        var dbOptions = new DbContextOptionsBuilder<RepositoryDBContext>()
            .UseNpgsql(repositoryConnectionString)
            .Options;

        var dbContext = new RepositoryDBContext(dbOptions);

        // Testing Connection to Repository
        Console.WriteLine("Testing database connection...");
        if(!await dbContext.Database.CanConnectAsync())
        {
            throw new InvalidOperationException("Cannot connect to Repository database");
        }

        Console.ForegroundColor= ConsoleColor.Green;
        Console.WriteLine("* Database connection successful");
        Console.ResetColor();

        // Create connection strings dictionary for execution databases
        var connectionStrings = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(wmsConnectionString))
        {
            connectionStrings["WMS"] = wmsConnectionString;
            connectionStrings["DEFAULT"] = wmsConnectionString; // Set WMS as default
        }

        if (!string.IsNullOrEmpty(engineConnectionString))
        {
            connectionStrings["ENGINE"] = engineConnectionString;
        }

        if (connectionStrings.Count == 0)
        {
            throw new InvalidOperationException("No execution database connection strings configured");
        }

        Console.WriteLine($"Configured databases: {string.Join(", ", connectionStrings.Keys)}");


        //Create Services
        var loaderService = new ApplicationLoaderService(dbContext);
        _engine = new ExecutionEngine(connectionStrings, loaderService);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("* Process Engine initialized successfully");
        Console.ResetColor();
        Console.WriteLine();
    }

    private static async Task LoadApplicationsAsync()
    {
        Console.WriteLine("Loadinjg applications...");

        var appGuidsConfig = _configuration.GetSection("Applications:LoadOnStartup").Get<string[]>() ?? Array.Empty<string>();

        if (appGuidsConfig == null || appGuidsConfig.Length == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("* No applications configured to load");
            Console.ResetColor();
            Console.WriteLine();
            return;
        }

        int successCount = 0;
        int failCount = 0;

        foreach (var appGuid in appGuidsConfig)
        {
            try
            {
                if(Guid.TryParse(appGuid, out var appId))
                {
                    await _engine.LoadApplicationAsync(appId);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Loaded application: {appId}");
                    Console.ResetColor();
                    successCount++;
                }

            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed to load application {appGuid}: {ex.Message}");
                Console.ResetColor();
                failCount++;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Applications loaded: {successCount} success, {failCount} failed");
        Console.WriteLine($"Total modules in cache: {_engine.Cache.Modules.Count}");
        Console.WriteLine($"  - Process Modules: {_engine.Cache.ProcessModules.Count}");
        Console.WriteLine($"  - Database Actions: {_engine.Cache.DatabaseActions.Count}");
        Console.WriteLine($"  - Field Modules: {_engine.Cache.FieldModules.Count}");
        Console.WriteLine();
    }

    private static async Task ShowMainMenuAsync()
    {
        while (true)
        {
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine("  MAIN MENU");
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine("1. Execute Process Module");
            Console.WriteLine("2. List Loaded Modules");
            Console.WriteLine("3. Test Concurrent Execution");
            Console.WriteLine("4. Reload Applications");
            Console.WriteLine("5. Exit");
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.Write("Select option: ");

            var choice = Console.ReadLine();

            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    await ExecuteProcessAsync();
                    break;
                case "2":
                    ListModules();
                    break;
                case "3":
                    await TestConcurrentExecutionAsync();
                    break;
                case "4":
                    await LoadApplicationsAsync();
                    break;
                case "5":
                    return;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid option. Please try again.");
                    Console.ResetColor();
                    Console.WriteLine();
                    break;
            }
        }
    }

    static async Task ExecuteProcessAsync()
    {
        Console.WriteLine("─────────────────────────────────────────────────────");
        Console.WriteLine("  EXECUTE PROCESS MODULE");
        Console.WriteLine("─────────────────────────────────────────────────────");


        // Get Process Module ID
        Console.WriteLine("Enter Process Module GUID: ");
        var guidInput = Console.ReadLine();

        if(!Guid.TryParse(guidInput, out var processModuleId))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Invalid GUID format");
            Console.ResetColor();
            Console.WriteLine();
            return;
        }

        // check if module exists
        var processModule = _engine.Cache.GetProcessModule(processModuleId);
        if (processModule == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Process Module {processModuleId} not found in cache");
            Console.ResetColor();
            Console.WriteLine();
            return;
        }

        Console.WriteLine($"Process Module: {processModule.Module?.Name ?? "Unknown"}");
        Console.WriteLine();

        // Get User ID
        Console.WriteLine("Enter User ID (or press Enter to skip): ");
        var userId = Console.ReadLine();
        
        if (string.IsNullOrWhiteSpace(userId)) userId = "console-user";

        // Get Parameters
        var parameters = new Dictionary<string, object>();
        Console.WriteLine("\n Enter parameters(leave blank to finish): Enter one paramter at a time");

        while (true)
        {
            Console.Write(" Parameter Name (or blank to finish): ");
            var paramName = Console.ReadLine();

            if (string.IsNullOrEmpty(paramName)) break;

            Console.Write($" Value for '{paramName}': ");
            var paramValue = Console.ReadLine();

            if(!string.IsNullOrEmpty(paramValue))
            {
                parameters[paramName] = paramValue;
            }
        }

        Console.WriteLine();
        Console.WriteLine("Executing process...");
        Console.WriteLine();

        var startTime = DateTime.UtcNow;

        try
        {
            var result = await _engine.ExecuteProcessModuleAsync(
                processModuleId,
                userId,
                parameters.Count > 0 ? parameters : null
            );

            var duration = DateTime.UtcNow - startTime;

            Console.WriteLine("─────────────────────────────────────────────────────");
            Console.WriteLine("  EXECUTION RESULT");
            Console.WriteLine("─────────────────────────────────────────────────────");

            if(result.Result == Domain.ProcessEngine.Enums.ExecutionResult.Pass)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Status: PASS");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Status: FAIL");
            }
            Console.ResetColor();

            Console.WriteLine($"Message: {result.Message}");
            Console.WriteLine($"Duration: {duration.TotalMicroseconds}ms");

            if (result.Exception != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nException: {result.Exception.Message}");
                Console.WriteLine($"Stack Trace: {result.Exception.StackTrace}");
                Console.ResetColor();
            }

            if (result.ReturnedFields.Count > 0)
            {
                Console.WriteLine("\nReturned Fields:");
                foreach (var field in result.ReturnedFields)
                {
                    var fieldModule = _engine.Cache.GetFieldModule(field.Key);
                    var fieldName = fieldModule?.Module?.Name ?? field.Key.ToString();
                    Console.WriteLine($"  {fieldName}: {field.Value}");
                }
            }
            Console.WriteLine("─────────────────────────────────────────────────────");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Execution Error: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            Console.ResetColor();
        }

        Console.WriteLine();
    }

    static void ListModules()
    {
        Console.WriteLine("─────────────────────────────────────────────────────");
        Console.WriteLine("  LOADED MODULES");
        Console.WriteLine("─────────────────────────────────────────────────────");

        Console.WriteLine($"\nTotal Modules: {_engine.Cache.Modules.Count}");
        Console.WriteLine();

        // List Process Modules
        foreach (var pm in _engine.Cache.ProcessModules.Values)
        {
            var module = pm.Module;
            Console.WriteLine($"  [{pm.ModuleId}] {module?.Name ?? "Unknown"}");
            Console.WriteLine($" Steps: {pm.Details?.Count ?? 0}");
        }

        Console.WriteLine();

        // List Database Actions
        Console.WriteLine("Database Action Modules:");
        foreach (var dam in _engine.Cache.DatabaseActions.Values)
        {
            var module = dam.Module;
            Console.WriteLine($" [{dam.ModuleId}] {module?.Name ?? "Unknown"}");
        }

        Console.WriteLine();


        // List Field Modules
        Console.WriteLine("Field Modules:");
        foreach (var fm in _engine.Cache.FieldModules.Values)
        {
            var module = fm.Module;
            Console.WriteLine($"  [{fm.ModuleId}] {module?.Name ?? "Unknown"} ({fm.FieldType})");
        }

        Console.WriteLine("─────────────────────────────────────────────────────");
        Console.WriteLine();
    }

    static async Task TestConcurrentExecutionAsync()
    {
        Console.WriteLine("─────────────────────────────────────────────────────");
        Console.WriteLine("  CONCURRENT EXECUTION TEST");
        Console.WriteLine("─────────────────────────────────────────────────────");

        Console.WriteLine("Enter Process Module GUID: ");
        var guidInput = Console.ReadLine();

        if (!Guid.TryParse(guidInput, out var processModuleId))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Invalid GUID format");
            Console.ResetColor();
            Console.WriteLine();
            return;
        }

        Console.Write("Number of concurrent executions (default 5): ");
        var countInput = Console.ReadLine();
        if (!int.TryParse(countInput, out var count) || count <= 0) count = 5;

        Console.WriteLine();
        Console.WriteLine($"Starting {count} concurrent executions ...");
        Console.WriteLine();

        var tasks = new List<Task<ActionResult>>();
        var startTime = DateTime.UtcNow;

        for (int i = 1; i <= count; i++)
        {
            var userId = $"user{i}";
            var parameters = new Dictionary<string, object>()
            {
                { "User ID", 100 + i}
            };

            var task = _engine.ExecuteProcessModuleAsync(processModuleId, userId, parameters);
            tasks.Add(task);

            Console.WriteLine($"Started execution {i} for {userId}");
        }

        Console.WriteLine();
        Console.WriteLine("Waiting for all executions to complete...");

        var results = await Task.WhenAll(tasks);
        var duration = DateTime.UtcNow - startTime;

        Console.WriteLine();
        Console.WriteLine("─────────────────────────────────────────────────────");
        Console.WriteLine("  RESULTS");
        Console.WriteLine("─────────────────────────────────────────────────────");

        int passCount = 0;
        int failCount = 0;

        for (int i = 0; i < results.Length; i++)
        {
            var result = results[i];
            if(result.Result == ExecutionResult.Pass)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  User {i + 1}: ✓ PASS - {result.Message}");
                passCount++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  User {i + 1}: ✗ FAIL - {result.Message}");
                failCount++;
            }
            Console.ResetColor();
        }

        Console.WriteLine();
        Console.WriteLine($"Total Duration: {duration.TotalSeconds:F2}s");
        Console.WriteLine($"Average per execution: {duration.TotalMilliseconds / count:F2}ms");
        Console.WriteLine($"Success: {passCount}, Failed: {failCount}");
        Console.WriteLine("─────────────────────────────────────────────────────");
        Console.WriteLine();
    }
}
