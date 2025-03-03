using CursorMCPMonitor.Configuration;
using CursorMCPMonitor.Interfaces;
using CursorMCPMonitor.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;
using System.IO;
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
        try
        {
            // Setup Serilog initially with basic console logging
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            Log.Information("Starting Cursor MCP Monitor...");

            var host = CreateHostBuilder(args).Build();
            var config = host.Services.GetRequiredService<IConfiguration>();
            var appConfig = AppConfig.Load(config);
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            var consoleOutput = host.Services.GetRequiredService<IConsoleOutputService>();
            var logMonitorService = host.Services.GetRequiredService<ILogMonitorService>();

            Console.OutputEncoding = Encoding.UTF8;
            consoleOutput.WriteHighlight("===", "Cursor AI MCP Log Monitor ===");
            consoleOutput.WriteInfo("Configuration:", $"Logs root: {appConfig.LogsRoot}");
            consoleOutput.WriteInfo("Configuration:", $"Polling interval: {appConfig.PollIntervalMs}ms");
            consoleOutput.WriteInfo("Configuration:", $"Log file pattern: {appConfig.LogPattern}");
            consoleOutput.WriteInfo("Configuration:", $"Verbosity level: {appConfig.Verbosity}");

            // Get services and configure them
            var logProcessorService = host.Services.GetRequiredService<ILogProcessorService>();
            
            // Apply filter and verbosity settings
            if (config["Filter"] != null)
            {
                string filter = config["Filter"]!;
                consoleOutput.WriteInfo("Configuration:", $"Log filter: {filter}");
                logProcessorService.SetFilter(filter);
            }
            
            // Apply verbosity level
            logProcessorService.SetVerbosityLevel(appConfig.Verbosity);
            
            // Start monitoring
            logMonitorService.StartMonitoring(appConfig.LogsRoot!, appConfig);
            
            // Keep the application running
            await host.RunAsync();
            
            return;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
            return;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .WriteTo.Console(outputTemplate: 
                    "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    Path.Combine(AppContext.BaseDirectory, "logs", "cursormonitor-.log"),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"))
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
