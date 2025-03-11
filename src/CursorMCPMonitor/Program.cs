using CursorMCPMonitor.Configuration;
using CursorMCPMonitor.Interfaces;
using CursorMCPMonitor.Services;
using Microsoft.Extensions.FileProviders;
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
            
            // Configure static files with the correct paths for both development and tool installation
            var staticFileProviders = new List<IFileProvider>();
            var staticFilePathsFound = false;
            
            // First, try the regular wwwroot folder (for development)
            var wwwrootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");
            logger.LogDebug("Checking for wwwroot at: {Path}", wwwrootPath);
            if (Directory.Exists(wwwrootPath))
            {
                logger.LogInformation("Using wwwroot folder at: {Path}", wwwrootPath);
                staticFileProviders.Add(new PhysicalFileProvider(wwwrootPath));
                staticFilePathsFound = true;
            }
            
            // Next, try the .NET tool staticwebassets folder (for installed tools)
            var toolLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
            if (!string.IsNullOrEmpty(toolLocation))
            {
                // Navigate up to the package root
                var directoryInfo = new DirectoryInfo(toolLocation);
                var packageRoot = directoryInfo?.Parent?.Parent?.Parent?.FullName;
                if (!string.IsNullOrEmpty(packageRoot))
                {
                    var staticAssetsPath = Path.Combine(packageRoot, "staticwebassets");
                    logger.LogDebug("Checking for staticwebassets at: {Path}", staticAssetsPath);
                    if (Directory.Exists(staticAssetsPath))
                    {
                        logger.LogInformation("Using .NET tool staticwebassets folder at: {Path}", staticAssetsPath);
                        staticFileProviders.Add(new PhysicalFileProvider(staticAssetsPath));
                        staticFilePathsFound = true;
                    }
                }
            }
            
            // Look for 'wwwroot' in the exact location specified for .NET tools
            var dotnetToolsRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".dotnet", "tools");
            
            // Try the direct tools path first
            var directToolsWwwrootPath = Path.Combine(dotnetToolsRoot, "wwwroot");
            logger.LogDebug("Checking for wwwroot at tools path: {Path}", directToolsWwwrootPath);
            if (Directory.Exists(directToolsWwwrootPath))
            {
                logger.LogInformation("Using direct tools wwwroot folder at: {Path}", directToolsWwwrootPath);
                staticFileProviders.Add(new PhysicalFileProvider(directToolsWwwrootPath));
                staticFilePathsFound = true;
            }
                
            // Try the .store path
            var staticWebAssetsPath = Path.Combine(
                dotnetToolsRoot, ".store", "cursormcpmonitor", "0.1.1", "cursormcpmonitor", "0.1.1", "tools", "net9.0", "any", "wwwroot");
                
            logger.LogDebug("Checking for wwwroot at .store path: {Path}", staticWebAssetsPath);
            if (Directory.Exists(staticWebAssetsPath))
            {
                logger.LogInformation("Using .store wwwroot folder at: {Path}", staticWebAssetsPath);
                staticFileProviders.Add(new PhysicalFileProvider(staticWebAssetsPath));
                staticFilePathsFound = true;
            }
            
            // Try staticwebassets path
            var staticwebassetPath = Path.Combine(
                dotnetToolsRoot, ".store", "cursormcpmonitor", "0.1.1", "cursormcpmonitor", "0.1.1", "staticwebassets");
            
            logger.LogDebug("Checking for staticwebassets at .store path: {Path}", staticwebassetPath);
            if (Directory.Exists(staticwebassetPath))
            {
                logger.LogInformation("Using .store staticwebassets folder at: {Path}", staticwebassetPath);
                staticFileProviders.Add(new PhysicalFileProvider(staticwebassetPath));
                staticFilePathsFound = true;
            }
            
            // Set up a backup static file provider from embedded resources if all else fails
            if (!staticFilePathsFound)
            {
                logger.LogWarning("No static file paths found, trying embedded resources.");
                var assembly = Assembly.GetExecutingAssembly();
                var embeddedProvider = new EmbeddedFileProvider(assembly, "CursorMCPMonitor.wwwroot");
                staticFileProviders.Add(embeddedProvider);
            }
            
            // If we have providers, use them - CRITICAL: Order matters for middleware
            // UseStaticFiles should come BEFORE UseDefaultFiles
            if (staticFileProviders.Count > 0)
            {
                // Create composite provider or use single provider
                IFileProvider provider = staticFileProviders.Count == 1 
                    ? staticFileProviders[0] 
                    : new CompositeFileProvider(staticFileProviders);
                
                // Configure static files
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = provider,
                    RequestPath = "" // Ensure no prefix
                });
                
                // Configure default files AFTER static files
                app.UseDefaultFiles(new DefaultFilesOptions
                {
                    FileProvider = provider,
                    DefaultFileNames = new List<string> { "index.html" }
                });
                
                // Add a fallback to index.html for SPA routing
                app.MapFallbackToFile("index.html", new StaticFileOptions
                {
                    FileProvider = provider
                });
                
                // Log the web interface URL
                consoleOutput.WriteSuccess("Web interface:", "http://localhost:5050");
                logger.LogInformation("Web interface available at: http://localhost:5050");
            }
            else
            {
                logger.LogError("No static web assets found. Web interface will not be available.");
                consoleOutput.WriteError("Error:", "No static web assets found. Web interface will not be available.");
            }
            
            // DEBUG: List contents of directories to help diagnose issues
            foreach (var path in new[] { wwwrootPath, staticWebAssetsPath, staticwebassetPath, directToolsWwwrootPath })
            {
                if (Directory.Exists(path))
                {
                    logger.LogDebug("Contents of {Path}:", path);
                    foreach (var file in Directory.GetFiles(path))
                    {
                        logger.LogDebug("  - {File}", Path.GetFileName(file));
                    }
                    foreach (var dir in Directory.GetDirectories(path))
                    {
                        logger.LogDebug("  - {Dir}/", Path.GetFileName(dir));
                    }
                }
            }
            
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
