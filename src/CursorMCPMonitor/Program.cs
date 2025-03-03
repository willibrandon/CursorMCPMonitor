﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;
using System.Text.RegularExpressions;
using CursorMCPMonitor.Configuration;

namespace CursorMCPMonitor;

/// <summary>
/// Main program class that monitors Cursor MCP log files and processes their contents.
/// Handles directory watching, log file detection, and real-time log processing.
/// </summary>
public partial class Program
{
    private static readonly Dictionary<string, FileSystemWatcher> _activeLogWatchers = [];
    private static readonly Dictionary<string, LogTailer> _logTailers = [];

    // Regex to parse lines of the form:
    // 2025-03-02 12:26:34.698 [info] a602: Handling CreateClient action
    [GeneratedRegex(@"^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}) \[(?<level>\w+)\]\s+(?<clientId>\w+):\s+(?<message>.*)$", RegexOptions.Compiled | RegexOptions.ExplicitCapture)]
    private static partial Regex LogLineRegex();

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

        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("=== Cursor AI MCP Log Monitor ===");
        Console.WriteLine($"Verbosity level: {appConfig.Verbosity}");
        Console.WriteLine($"Poll interval: {appConfig.PollIntervalMs}ms");
        Console.WriteLine($"Log file pattern: {appConfig.LogPattern}");

        // Get the logs root directory from configuration
        if (!Directory.Exists(appConfig.LogsRoot))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Log root does not exist: {appConfig.LogsRoot}");
            Console.ResetColor();
            return;
        }

        // Monitor the root logs directory for new subdirectories
        var rootWatcher = new FileSystemWatcher(appConfig.LogsRoot)
        {
            NotifyFilter = NotifyFilters.DirectoryName,
            IncludeSubdirectories = false,
            EnableRaisingEvents = true
        };

        rootWatcher.Created += (_, e) =>
        {
            if (Directory.Exists(e.FullPath))
            {
                HandleNewLogSubdirectory(e.FullPath, appConfig);
            }
        };

        // Handle any existing subdirectories
        foreach (var subDir in Directory.GetDirectories(appConfig.LogsRoot))
        {
            HandleNewLogSubdirectory(subDir, appConfig);
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Monitoring for new subdirectories in: {appConfig.LogsRoot}");
        Console.WriteLine($"Looking for '{appConfig.LogPattern}' files in exthost/anysphere.cursor-always-local...");
        Console.ResetColor();

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
            });

    private static void HandleNewLogSubdirectory(string subDirPath, AppConfig appConfig)
    {
        lock (_activeLogWatchers)
        {
            if (_activeLogWatchers.ContainsKey(subDirPath))
                return;
        }

        var watcher = new FileSystemWatcher(subDirPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName
        };

        watcher.Created += (_, e) => OnSubdirectoryFileCreated(subDirPath, e, appConfig);
        watcher.EnableRaisingEvents = true;

        lock (_activeLogWatchers)
        {
            _activeLogWatchers[subDirPath] = watcher;
        }

        CheckForExistingCursorLog(subDirPath, appConfig);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[Subdirectory] Monitoring new subdirectory: {subDirPath}");
        Console.ResetColor();
    }

    private static void OnSubdirectoryFileCreated(string rootSubDir, FileSystemEventArgs e, AppConfig appConfig)
    {
        if (!File.Exists(e.FullPath))
            return;

        var relative = Path.GetRelativePath(rootSubDir, e.FullPath);
        // Use more flexible pattern matching based on appConfig.LogPattern
        if (relative.Replace('\\', '/').Contains("/window") && 
            relative.Replace('\\', '/').EndsWith($"exthost/anysphere.cursor-always-local/{appConfig.LogPattern}",
            StringComparison.OrdinalIgnoreCase))
        {
            StartTailer(e.FullPath, appConfig);
        }
    }

    private static void CheckForExistingCursorLog(string subDirPath, AppConfig appConfig)
    {
        // Check all window* subdirectories
        foreach (var windowDir in Directory.GetDirectories(subDirPath, "window*"))
        {
            var exthostPath = Path.Combine(windowDir, "exthost", "anysphere.cursor-always-local");
            var logFile = Path.Combine(exthostPath, appConfig.LogPattern);
            if (File.Exists(logFile))
            {
                StartTailer(logFile, appConfig);
            }
        }
    }

    private static void StartTailer(string fullFilePath, AppConfig appConfig)
    {
        lock (_logTailers)
        {
            if (_logTailers.ContainsKey(fullFilePath))
                return;

            var tailer = new LogTailer(fullFilePath, ProcessLogLine, appConfig.PollIntervalMs);
            _logTailers[fullFilePath] = tailer;
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[LogTailer] Now tailing: {fullFilePath}");
        Console.ResetColor();
    }

    private static void ProcessLogLine(string fullFilePath, string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        var match = LogLineRegex().Match(line);
        if (!match.Success)
        {
            // Handle unstructured lines
            if (line.Contains("No server info found", StringComparison.OrdinalIgnoreCase))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[NoServerInfo] {line}");
            }
            else if (line.Contains("unrecognized_keys", StringComparison.OrdinalIgnoreCase))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[UnrecognizedKeys] {line}");
            }
            else if (line.Contains("No workspace folders found", StringComparison.OrdinalIgnoreCase))
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"[Warning] {line}");
            }
            else
            {
                Console.WriteLine($"[Raw] {line}");
            }
            Console.ResetColor();
            return;
        }

        var timestamp = match.Groups["timestamp"].Value;
        var level = match.Groups["level"].Value;
        var clientId = match.Groups["clientId"].Value;
        var message = match.Groups["message"].Value;

        // Process different message types
        if (message.Contains("CreateClient action", StringComparison.OrdinalIgnoreCase))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[{timestamp}] [CreateClient] [Client: {clientId}] => {message}");
        }
        else if (message.Contains("ListOfferings action", StringComparison.OrdinalIgnoreCase))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[{timestamp}] [ListOfferings] [Client: {clientId}] => {message}");
        }
        else if (message.Contains("Error in MCP:", StringComparison.OrdinalIgnoreCase))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{timestamp}] [Error] [Client: {clientId}] => {message}");
        }
        else if (message.Contains("Client closed for command", StringComparison.OrdinalIgnoreCase))
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"[{timestamp}] [ClientClosed] [Client: {clientId}] => {message}");
        }
        else if (message.Contains("Successfully connected to stdio server", StringComparison.OrdinalIgnoreCase))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[{timestamp}] [Connected] [Client: {clientId}] => {message}");
        }
        else
        {
            var color = level.Equals("error", StringComparison.OrdinalIgnoreCase)
                ? ConsoleColor.Red
                : (level.Equals("warning", StringComparison.OrdinalIgnoreCase)
                    ? ConsoleColor.DarkYellow
                    : ConsoleColor.Gray);
            Console.ForegroundColor = color;
            Console.WriteLine($"[{timestamp}] [{level}] [Client: {clientId}] => {message}");
        }
        Console.ResetColor();
    }
}
