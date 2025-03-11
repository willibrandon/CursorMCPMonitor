using CursorMCPMonitor.Configuration;
using CursorMCPMonitor.Interfaces;
using CursorMCPMonitor.Services;
using Serilog;
using System.Reflection;
using System.Text;

namespace CursorMCPMonitor;

/// <summary>
/// Main program class that sets up dependency injection and starts the application.
/// </summary>
public class Program
{
    /// <summary>
    /// Entry point of the application. Initializes the log monitoring system and starts watching for changes.
    /// </summary>
    /// <param name="args">Command line arguments passed to the application.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task Main(string[] args)
    {
        // Simple early check for help and version
        if (args.Length == 1)
        {
            if (args[0] == "--version" || args[0] == "-v" || args[0] == "-V")
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                Console.WriteLine($"Cursor MCP Monitor version {version}");
                return;
            }
            
            if (args[0] == "--help" || args[0] == "-h" || args[0] == "-?")
            {
                Console.WriteLine("Cursor MCP Monitor - Real-time monitoring of Model Context Protocol interactions");
                Console.WriteLine();
                Console.WriteLine("Usage:");
                Console.WriteLine("  cursor-mcp [options]");
                Console.WriteLine();
                Console.WriteLine("Options:");
                Console.WriteLine("  -l, --logs-root <path>          Root directory containing Cursor logs");
                Console.WriteLine("  -p, --poll-interval <ms>        Polling interval in milliseconds");
                Console.WriteLine("  -v, --verbosity <level>         Log verbosity level (debug, info, warning, error)");
                Console.WriteLine("  -f, --log-pattern <pattern>     Log file pattern to monitor");
                Console.WriteLine("  --filter <text>                 Filter log messages containing specific text");
                Console.WriteLine("  --version                       Show version information");
                Console.WriteLine("  -?, -h, --help                  Show help and usage information");
                return;
            }
        }

        try
        {
            // Setup Serilog initially with basic console logging
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            Log.Information("Starting Cursor MCP Monitor...");

            var builder = WebApplication.CreateBuilder(args);
            
            // Configure web server to use port 5050
            builder.WebHost.UseUrls("http://localhost:5050");
            
            // Add services
            builder.Services.AddSingleton<IConsoleOutputService, ConsoleOutputService>();
            builder.Services.AddSingleton<IWebSocketService, WebSocketService>();
            builder.Services.AddSingleton<ILogProcessorService, LogProcessorService>();
            builder.Services.AddSingleton<ILogMonitorService, LogMonitorService>();

            // Configure logging
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog();

            var app = builder.Build();
            var config = app.Services.GetRequiredService<IConfiguration>();
            var appConfig = AppConfig.Load(config);
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            var consoleOutput = app.Services.GetRequiredService<IConsoleOutputService>();
            var logMonitorService = app.Services.GetRequiredService<ILogMonitorService>();
            var webSocketService = app.Services.GetRequiredService<IWebSocketService>();

            // Setup graceful shutdown
            var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
            lifetime.ApplicationStopping.Register(() =>
            {
                logMonitorService.Dispose();
                webSocketService.Dispose();
            });

            Console.OutputEncoding = Encoding.UTF8;
            consoleOutput.WriteHighlight("===", "Cursor AI MCP Log Monitor ===");
            consoleOutput.WriteInfo("Configuration:", $"Logs root: {appConfig.LogsRoot}");
            consoleOutput.WriteInfo("Configuration:", $"Polling interval: {appConfig.PollIntervalMs}ms");
            consoleOutput.WriteInfo("Configuration:", $"Log file pattern: {appConfig.LogPattern}");
            consoleOutput.WriteInfo("Configuration:", $"Verbosity level: {appConfig.Verbosity}");

            // Get services and configure them
            var logProcessorService = app.Services.GetRequiredService<ILogProcessorService>();
            
            // Apply filter and verbosity settings
            if (config["Filter"] != null)
            {
                string filter = config["Filter"]!;
                consoleOutput.WriteInfo("Configuration:", $"Log filter: {filter}");
                logProcessorService.SetFilter(filter);
            }
            
            // Apply verbosity level
            logProcessorService.SetVerbosityLevel(appConfig.Verbosity);
            
            // Configure web server
            app.UseWebSockets();
            
            // WebSocket endpoint
            app.Map("/ws", async context =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    await webSocketService.HandleClientAsync(webSocket);
                }
                else
                {
                    context.Response.StatusCode = 400;
                }
            });

            // Serve static files from wwwroot
            app.UseDefaultFiles();
            app.UseStaticFiles();

            // Start monitoring
            logMonitorService.StartMonitoring(appConfig.LogsRoot!, appConfig);
            
            // Start the web server
            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
