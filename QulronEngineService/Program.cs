
using Infrastructure.Presistence;
using Infrastructure.ProcessEngine;
using Infrastructure.ProcessEngine.Services;
using Infrastructure.ProcessEngine.Session;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace QulronEngineService;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi();
        

        // Add CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        // Add DbContext
        var connectionString = builder.Configuration.GetConnectionString("RepositoryDB");
        builder.Services.AddDbContext<RepositoryDBContext>(options =>
            options.UseNpgsql(connectionString));

        // Add Engine Services (Singleton - shared across all requests)
        builder.Services.AddSingleton<EngineSessionManager>();

        builder.Services.AddSingleton<ExecutionEngine>(provider =>
        {
            var dbContext = provider.GetRequiredService<RepositoryDBContext>();
            var connectionStrings = new Dictionary<string, string>();

            var wmsConnection = builder.Configuration.GetConnectionString("WMS");
            var engineConnection = builder.Configuration.GetConnectionString("Engine");

            if(!string.IsNullOrEmpty(wmsConnection))
            {
                connectionStrings["WMS"] = wmsConnection;
                connectionStrings["DEFAULT"] = wmsConnection;
            }

            if(!string.IsNullOrEmpty(engineConnection))
            {
                connectionStrings["ENGINE"] = engineConnection;
            }

            var loaderService = new ApplicationLoaderService(dbContext);
            return new ExecutionEngine(connectionStrings, loaderService);

        });

        builder.Services.AddScoped<DeviceInitializationService>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseCors("AllowAll");
        app.UseAuthorization();
        app.MapControllers();

        // Initialize Engine at Startup
        using (var scope = app.Services.CreateScope())
        {
            try
            {
                Console.WriteLine();
                Console.WriteLine("╔════════════════════════════════════════════════════════╗");
                Console.WriteLine("║     QULRON ENGINE SERVICE - STARTUP                    ║");
                Console.WriteLine("╚════════════════════════════════════════════════════════╝");
                Console.WriteLine();

                // Get services
                var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
                var executionEngine = scope.ServiceProvider.GetRequiredService<ExecutionEngine>();
                var sessionManager = scope.ServiceProvider.GetRequiredService<EngineSessionManager>();

                // Test Repository Database Connection
                Console.WriteLine("Testing database connection...");
                if(!await dbContext.Database.CanConnectAsync())
                {
                    throw new InvalidOperationException("Cannot connect to Repository database");
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Database connection successful\n");
                Console.ResetColor();

                // Load applications
                Console.WriteLine("Loading Applications ...");
                var appGuidsConfig = builder.Configuration // Get the application list from Configuration
                    .GetSection("Applications:LoadOnStartup")
                    .Get<string[]>() ?? Array.Empty<string>();


                if (appGuidsConfig == null || appGuidsConfig.Length == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("* No applications configured to load");
                    Console.ResetColor();
                    Console.WriteLine();
                }
                else if (appGuidsConfig.Length > 0)
                {
                    foreach(var appGuid in appGuidsConfig)
                    {
                        if (Guid.TryParse(appGuid, out var appId))
                        {
                            await executionEngine.LoadApplicationAsync(appId);
                            Console.ForegroundColor= ConsoleColor.Green;
                            Console.WriteLine("Loaded applicatoin: {appId}");
                            Console.ResetColor();
                        }
                    }
                }

                Console.WriteLine();
                Console.WriteLine($"Total modules in cache: {executionEngine.Cache.Modules.Count}");
                Console.WriteLine($" - Process Modules: {executionEngine.Cache.ProcessModules.Count}");
                Console.WriteLine($" - Database Actions: {executionEngine.Cache.DatabaseActions.Count}");
                Console.WriteLine($" - Field Modules: {executionEngine.Cache.FieldModules.Count}");
                Console.WriteLine($" - Dialog Actions: {executionEngine.Cache.DialogActions.Count}");

                // Initialize devices
                var deviceService = scope.ServiceProvider.GetRequiredService<DeviceInitializationService>();
                await deviceService.InitializeAllDevicesAsync();

                Console.WriteLine();
                Console.WriteLine("╔════════════════════════════════════════════════════════╗");
                Console.WriteLine("║     ENGINE READY - API SERVICE STARTED                 ║");
                Console.WriteLine("╚════════════════════════════════════════════════════════╝");


            } catch (Exception ex) 
            {
            
            }
            
        }


        app.Run("http://localhost:5000");
    }
}
