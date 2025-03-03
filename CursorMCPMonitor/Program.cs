using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;
using System.Text.RegularExpressions;

namespace CursorMCPMonitor;

public class Program
{
    private static readonly Dictionary<string, FileSystemWatcher> _activeLogWatchers = new();
    private static readonly Dictionary<string, LogTailer> _logTailers = new();

    // Regex to parse lines of the form:
    // 2025-03-02 12:26:34.698 [info] a602: Handling CreateClient action
    private static readonly Regex LogLineRegex = new(
        @"^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}) \[(?<level>\w+)\]\s+(?<clientId>\w+):\s+(?<message>.*)$",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        var config = host.Services.GetRequiredService<IConfiguration>();

        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("=== Cursor AI MCP Log Monitor ===");

        // Get the logs root directory from configuration or environment
        var logsRoot = config.GetValue<string>("LogsRoot") ?? 
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Cursor",
                "logs"
            );

        if (!Directory.Exists(logsRoot))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Log root does not exist: {logsRoot}");
            Console.ResetColor();
            return;
        }

        // Monitor the root logs directory for new subdirectories
        var rootWatcher = new FileSystemWatcher(logsRoot)
        {
            NotifyFilter = NotifyFilters.DirectoryName,
            IncludeSubdirectories = false,
            EnableRaisingEvents = true
        };

        rootWatcher.Created += (_, e) =>
        {
            if (Directory.Exists(e.FullPath))
            {
                HandleNewLogSubdirectory(e.FullPath);
            }
        };

        // Handle any existing subdirectories
        foreach (var subDir in Directory.GetDirectories(logsRoot))
        {
            HandleNewLogSubdirectory(subDir);
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Monitoring for new subdirectories in: {logsRoot}");
        Console.WriteLine("Looking for 'Cursor MCP.log' files in exthost/anysphere.cursor-always-local...");
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
                      .AddEnvironmentVariables()
                      .AddCommandLine(args);
            });

    private static void HandleNewLogSubdirectory(string subDirPath)
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

        watcher.Created += (_, e) => OnSubdirectoryFileCreated(subDirPath, e);
        watcher.EnableRaisingEvents = true;

        lock (_activeLogWatchers)
        {
            _activeLogWatchers[subDirPath] = watcher;
        }

        CheckForExistingCursorLog(subDirPath);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[Subdirectory] Monitoring new subdirectory: {subDirPath}");
        Console.ResetColor();
    }

    private static void OnSubdirectoryFileCreated(string rootSubDir, FileSystemEventArgs e)
    {
        if (!File.Exists(e.FullPath))
            return;

        var relative = Path.GetRelativePath(rootSubDir, e.FullPath);
        if (relative.Replace('\\', '/').Contains("/window") && 
            relative.Replace('\\', '/').EndsWith("exthost/anysphere.cursor-always-local/Cursor MCP.log",
            StringComparison.OrdinalIgnoreCase))
        {
            StartTailer(e.FullPath);
        }
    }

    private static void CheckForExistingCursorLog(string subDirPath)
    {
        // Check all window* subdirectories
        foreach (var windowDir in Directory.GetDirectories(subDirPath, "window*"))
        {
            var exthostPath = Path.Combine(windowDir, "exthost", "anysphere.cursor-always-local");
            var logFile = Path.Combine(exthostPath, "Cursor MCP.log");
            if (File.Exists(logFile))
            {
                StartTailer(logFile);
            }
        }
    }

    private static void StartTailer(string fullFilePath)
    {
        lock (_logTailers)
        {
            if (_logTailers.ContainsKey(fullFilePath))
                return;

            var tailer = new LogTailer(fullFilePath, ProcessLogLine);
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

        var match = LogLineRegex.Match(line);
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
