using CursorMCPMonitor.Configuration;
using CursorMCPMonitor.Interfaces;
using Microsoft.Extensions.Logging;

namespace CursorMCPMonitor.Services;

/// <summary>
/// Service responsible for monitoring log directories and files,
/// detecting new log files, and managing log tailers.
/// </summary>
public class LogMonitorService : ILogMonitorService
{
    private readonly Dictionary<string, FileSystemWatcher> _activeLogWatchers = [];
    private readonly Dictionary<string, LogTailer> _logTailers = [];
    private readonly ILogProcessorService _logProcessor;
    private readonly ILogger<LogMonitorService> _logger;
    private readonly IConsoleOutputService _consoleOutput;

    public LogMonitorService(
        ILogProcessorService logProcessor,
        ILogger<LogMonitorService> logger,
        IConsoleOutputService consoleOutput)
    {
        _logProcessor = logProcessor;
        _logger = logger;
        _consoleOutput = consoleOutput;
    }

    /// <summary>
    /// Starts monitoring the root logs directory for new subdirectories.
    /// </summary>
    /// <param name="rootLogDirectory">The root directory to monitor</param>
    /// <param name="appConfig">Application configuration</param>
    public void StartMonitoring(string rootLogDirectory, AppConfig appConfig)
    {
        if (string.IsNullOrEmpty(rootLogDirectory))
        {
            _logger.LogError("Log root directory path is null or empty");
            _consoleOutput.WriteError("Error:", "Log root directory path is null or empty");
            return;
        }

        if (!Directory.Exists(rootLogDirectory))
        {
            _logger.LogError("Log root does not exist: {LogRoot}", rootLogDirectory);
            _consoleOutput.WriteError("Error:", $"Log root does not exist: {rootLogDirectory}");
            return;
        }

        _logger.LogInformation("Starting monitoring of root directory: {RootDir}", rootLogDirectory);
        _consoleOutput.WriteSuccess("Monitoring:", $"Root directory: {rootLogDirectory}");
        _consoleOutput.WriteInfo("Looking for:", $"'{appConfig.LogPattern}' files in exthost/anysphere.cursor-always-local");

        // Check for existing log subdirectories
        foreach (var subDir in Directory.GetDirectories(rootLogDirectory))
        {
            HandleNewLogSubdirectory(subDir, appConfig);
        }

        // Set up a watcher for new subdirectories
        var rootWatcher = new FileSystemWatcher(rootLogDirectory)
        {
            NotifyFilter = NotifyFilters.DirectoryName,
            IncludeSubdirectories = false
        };

        rootWatcher.Created += (_, e) =>
        {
            if (Directory.Exists(e.FullPath))
            {
                _logger.LogDebug("New directory detected: {Directory}", e.FullPath);
                HandleNewLogSubdirectory(e.FullPath, appConfig);
            }
        };

        rootWatcher.EnableRaisingEvents = true;
        _logger.LogInformation("Root directory watcher enabled for {Directory}", rootLogDirectory);
    }

    /// <summary>
    /// Handles a new log subdirectory by setting up a watcher and checking for existing log files.
    /// </summary>
    /// <param name="subDirPath">Path to the new subdirectory</param>
    /// <param name="appConfig">Application configuration</param>
    private void HandleNewLogSubdirectory(string subDirPath, AppConfig appConfig)
    {
        lock (_activeLogWatchers)
        {
            if (_activeLogWatchers.ContainsKey(subDirPath))
            {
                _logger.LogDebug("Subdirectory already being monitored: {SubDir}", subDirPath);
                return;
            }
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
        
        _logger.LogInformation("Monitoring new subdirectory: {SubDir}", subDirPath);
        _consoleOutput.WriteSuccess("Subdirectory:", $"Monitoring {subDirPath}");
    }

    /// <summary>
    /// Event handler for file creation in monitored subdirectories.
    /// </summary>
    private void OnSubdirectoryFileCreated(string rootSubDir, FileSystemEventArgs e, AppConfig appConfig)
    {
        if (!File.Exists(e.FullPath))
        {
            _logger.LogDebug("File creation event triggered but file does not exist: {FilePath}", e.FullPath);
            return;
        }

        var relative = Path.GetRelativePath(rootSubDir, e.FullPath);
        // Use more flexible pattern matching based on appConfig.LogPattern
        if (relative.Replace('\\', '/').Contains("/window") && 
            relative.Replace('\\', '/').EndsWith($"exthost/anysphere.cursor-always-local/{appConfig.LogPattern}",
            StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Detected new Cursor MCP log file: {FilePath}", e.FullPath);
            StartTailer(e.FullPath, appConfig);
        }
        else
        {
            _logger.LogDebug("File created but does not match log pattern: {FilePath}", e.FullPath);
        }
    }

    /// <summary>
    /// Checks for existing Cursor log files in a subdirectory.
    /// </summary>
    private void CheckForExistingCursorLog(string subDirPath, AppConfig appConfig)
    {
        _logger.LogDebug("Checking for existing log files in {SubDir}", subDirPath);
        
        // Check all window* subdirectories
        foreach (var windowDir in Directory.GetDirectories(subDirPath, "window*"))
        {
            var exthostPath = Path.Combine(windowDir, "exthost", "anysphere.cursor-always-local");
            var logFile = Path.Combine(exthostPath, appConfig.LogPattern);
            
            if (File.Exists(logFile))
            {
                _logger.LogInformation("Found existing log file: {LogFile}", logFile);
                StartTailer(logFile, appConfig);
            }
        }
    }

    /// <summary>
    /// Starts a log tailer for a specific log file.
    /// </summary>
    private void StartTailer(string fullFilePath, AppConfig appConfig)
    {
        lock (_logTailers)
        {
            if (_logTailers.ContainsKey(fullFilePath))
            {
                _logger.LogDebug("Log file already being tailed: {LogFile}", fullFilePath);
                return;
            }

            var tailer = new LogTailer(fullFilePath, _logProcessor.ProcessLogLine, appConfig.PollIntervalMs);
            _logTailers[fullFilePath] = tailer;
        }

        _logger.LogInformation("Started tailing: {LogFile} with poll interval {PollInterval}ms", 
            fullFilePath, appConfig.PollIntervalMs);
        _consoleOutput.WriteSuccess("LogTailer:", $"Now tailing: {fullFilePath}");
    }

    /// <summary>
    /// Stops all active watchers and tailers.
    /// </summary>
    public void Dispose()
    {
        _logger.LogInformation("Disposing LogMonitorService, stopping all watchers and tailers");
        
        lock (_activeLogWatchers)
        {
            foreach (var watcher in _activeLogWatchers.Values)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            _activeLogWatchers.Clear();
        }

        lock (_logTailers)
        {
            foreach (var tailer in _logTailers.Values)
            {
                tailer.Stop();
            }
            _logTailers.Clear();
        }
        
        _logger.LogInformation("All watchers and tailers stopped");
    }
} 