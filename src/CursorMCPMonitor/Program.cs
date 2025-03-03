using CursorMCPMonitor.Configuration;
using CursorMCPMonitor.Interfaces;
using CursorMCPMonitor.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        var host = CreateHostBuilder(args).Build();
        var config = host.Services.GetRequiredService<IConfiguration>();
        var appConfig = AppConfig.Load(config);
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var consoleOutput = host.Services.GetRequiredService<IConsoleOutputService>();
        var logMonitor = host.Services.GetRequiredService<ILogMonitorService>();

        Console.OutputEncoding = Encoding.UTF8;
        consoleOutput.WriteHighlight("===", "Cursor AI MCP Log Monitor ===");
        consoleOutput.WriteInfo("Configuration:", $"Verbosity level: {appConfig.Verbosity}");
        consoleOutput.WriteInfo("Configuration:", $"Poll interval: {appConfig.PollIntervalMs}ms");
        consoleOutput.WriteInfo("Configuration:", $"Log file pattern: {appConfig.LogPattern}");

        // Get the logs root directory from configuration
        var logsRoot = string.IsNullOrEmpty(appConfig.LogsRoot)
            ? AppConfig.GetDefaultLogsDirectory()
            : appConfig.LogsRoot;
            
        logger.LogInformation("Using logs root directory: {LogsRoot}", logsRoot);

        // Start monitoring the logs directory
        logMonitor.StartMonitoring(logsRoot, appConfig);
        
        // Keep the application running
        await host.RunAsync();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory())
                      .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                      .AddEnvironmentVariables();
                      
                // Add command line arguments
                var cmdLineArgs = CommandLineOptions.Parse(args);
                if (cmdLineArgs.Count > 0)
                {
                    config.AddInMemoryCollection(cmdLineArgs);
                }
            })
            .ConfigureServices((hostContext, services) =>
            {
                // Register services
                services.AddSingleton<IConsoleOutputService, ConsoleOutputService>();
                services.AddSingleton<ILogProcessorService, LogProcessorService>();
                services.AddSingleton<ILogMonitorService, LogMonitorService>();
            });
}
