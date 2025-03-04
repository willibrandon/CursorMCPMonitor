using System.Text.RegularExpressions;
using CursorMCPMonitor.Interfaces;
using Microsoft.Extensions.Logging;

namespace CursorMCPMonitor.Services;

/// <summary>
/// Service for processing log lines from Cursor MCP log files.
/// Handles parsing structured log lines and special case handling.
/// </summary>
public partial class LogProcessorService : ILogProcessorService
{
    private readonly IConsoleOutputService _consoleOutput;
    private readonly ILogger<LogProcessorService> _logger;
    private readonly IWebSocketService _webSocketService;
    private string? _filterPattern;
    private LogLevel _verbosityLevel = LogLevel.Debug;

    // Regex to parse lines of the form:
    // 2025-03-02 12:26:34.698 [info] a602: Handling CreateClient action
    [GeneratedRegex(@"^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}) \[(?<level>\w+)\]\s+(?<clientId>\w+):\s+(?<message>.*)$", RegexOptions.Compiled | RegexOptions.ExplicitCapture)]
    private static partial Regex LogLineRegex();

    public LogProcessorService(
        IConsoleOutputService consoleOutput,
        ILogger<LogProcessorService> logger,
        IWebSocketService webSocketService)
    {
        _consoleOutput = consoleOutput;
        _logger = logger;
        _webSocketService = webSocketService;
    }

    /// <summary>
    /// Sets or updates the filter pattern to be applied to log output
    /// </summary>
    /// <param name="filterPattern">Text pattern to filter logs (can be null to disable filtering)</param>
    public void SetFilter(string? filterPattern)
    {
        _filterPattern = filterPattern;
        _logger.LogInformation("Log filter set to: {FilterPattern}", filterPattern ?? "(none)");
    }

    /// <summary>
    /// Sets the minimum verbosity level to display
    /// </summary>
    /// <param name="level">Minimum log level to display (Debug, Info, Warning, Error)</param>
    public void SetVerbosityLevel(string level)
    {
        _verbosityLevel = ParseLogLevel(level);
        _logger.LogInformation("Log verbosity set to: {Level}", _verbosityLevel);
    }

    /// <summary>
    /// Parses a log level string to the corresponding LogLevel enum value
    /// </summary>
    private LogLevel ParseLogLevel(string level)
    {
        return level?.ToLower() switch
        {
            "debug" => LogLevel.Debug,
            "information" or "info" => LogLevel.Information,
            "warning" or "warn" => LogLevel.Warning,
            "error" or "err" => LogLevel.Error,
            _ => LogLevel.Information
        };
    }

    /// <summary>
    /// Converts a log level string from the log file to the corresponding LogLevel enum value
    /// </summary>
    private static LogLevel? ConvertLogLevel(string level)
    {
        return level?.ToLower() switch
        {
            "debug" => LogLevel.Debug,
            "info" => LogLevel.Information,
            "warning" or "warn" => LogLevel.Warning,
            "error" or "err" => LogLevel.Error,
            _ => null
        };
    }

    /// <summary>
    /// Processes a line from a log file, parsing it and directing it to the appropriate output.
    /// </summary>
    /// <param name="fullFilePath">The full path to the log file</param>
    /// <param name="line">The line of text from the log file</param>
    public void ProcessLogLine(string fullFilePath, string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        // Apply filter if one is set
        if (!string.IsNullOrEmpty(_filterPattern) && 
            !line.Contains(_filterPattern, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var match = LogLineRegex().Match(line);
        if (!match.Success)
        {
            ProcessUnstructuredLine(fullFilePath, line);
            return;
        }

        var timestamp = match.Groups["timestamp"].Value;
        var level = match.Groups["level"].Value;
        var clientId = match.Groups["clientId"].Value;
        var message = match.Groups["message"].Value;

        // Apply verbosity filter for structured logs
        var logLevel = ConvertLogLevel(level);
        if (logLevel == null)
        {
            ProcessUnstructuredLine(fullFilePath, line);
            return;
        }

        if (_verbosityLevel != LogLevel.Debug && logLevel < _verbosityLevel)
        {
            // When Debug is selected, show everything
            // For other levels, filter according to severity (but with correct logic)
            return;
        }

        _logger.LogDebug("Processing log line from {LogFile}: {Timestamp} [{Level}] {ClientId}: {Message}", 
            Path.GetFileName(fullFilePath), timestamp, level, clientId, message);

        ProcessStructuredLine(fullFilePath, timestamp, level, clientId, message);
    }

    /// <summary>
    /// Processes an unstructured log line (doesn't match the standard log format).
    /// </summary>
    /// <param name="fullFilePath">The full path to the log file</param>
    /// <param name="line">The unstructured log line</param>
    private void ProcessUnstructuredLine(string fullFilePath, string line)
    {
        var fileName = Path.GetFileName(fullFilePath);
        var logEvent = new
        {
            Type = "UnstructuredLog",
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            FileName = fileName,
            Message = line
        };
        
        // Handle unstructured lines
        if (line.Contains("No server info found", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError("NoServerInfo from {LogFile}: {Message}", fileName, line);
            _consoleOutput.WriteError("[NoServerInfo]", line);
            logEvent = new { Type = "NoServerInfo", Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), FileName = fileName, Message = line };
        }
        else if (line.Contains("unrecognized_keys", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError("UnrecognizedKeys from {LogFile}: {Message}", fileName, line);
            _consoleOutput.WriteError("[UnrecognizedKeys]", line);
            logEvent = new { Type = "UnrecognizedKeys", Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), FileName = fileName, Message = line };
        }
        else if (line.Contains("No workspace folders found", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("NoWorkspace from {LogFile}: {Message}", fileName, line);
            _consoleOutput.WriteWarning("[Warning]", line);
            logEvent = new { Type = "NoWorkspace", Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), FileName = fileName, Message = line };
        }
        else
        {
            _logger.LogInformation("RawLine from {LogFile}: {Message}", fileName, line);
            _consoleOutput.WriteRaw("[Raw]", line);
        }

        _webSocketService.BroadcastAsync(logEvent).ConfigureAwait(false);
    }

    /// <summary>
    /// Processes a structured log line (matching the standard log format).
    /// </summary>
    private void ProcessStructuredLine(string fullFilePath, string timestamp, string level, string clientId, string message)
    {
        var fileName = Path.GetFileName(fullFilePath);
        var logEvent = new
        {
            Type = "StructuredLog",
            Timestamp = timestamp,
            Level = level,
            ClientId = clientId,
            Message = message,
            FileName = fileName
        };
        
        // Process different message types
        if (message.Contains("CreateClient action", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("{EventType} detected: {LogFile} {Timestamp} [{Level}] {ClientId}: {Message}", 
                "CreateClient", fileName, timestamp, level, clientId, message);
            _consoleOutput.WriteSuccess($"[{timestamp}]", $"[CreateClient] [Client: {clientId}] => {message}");
            logEvent = new { Type = "CreateClient", Timestamp = timestamp, Level = level, ClientId = clientId, Message = message, FileName = fileName };
        }
        else if (message.Contains("ListOfferings action", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("{EventType} detected: {LogFile} {Timestamp} [{Level}] {ClientId}: {Message}", 
                "ListOfferings", fileName, timestamp, level, clientId, message);
            _consoleOutput.WriteHighlight($"[{timestamp}]", $"[ListOfferings] [Client: {clientId}] => {message}");
            logEvent = new { Type = "ListOfferings", Timestamp = timestamp, Level = level, ClientId = clientId, Message = message, FileName = fileName };
        }
        else if (message.Contains("Error in MCP:", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError("{EventType} detected: {LogFile} {Timestamp} [{Level}] {ClientId}: {Message}", 
                "MCPError", fileName, timestamp, level, clientId, message);
            _consoleOutput.WriteError($"[{timestamp}]", $"[Error] [Client: {clientId}] => {message}");
            logEvent = new { Type = "MCPError", Timestamp = timestamp, Level = level, ClientId = clientId, Message = message, FileName = fileName };
        }
        else if (message.Contains("Client closed for command", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("{EventType} detected: {LogFile} {Timestamp} [{Level}] {ClientId}: {Message}", 
                "ClientClosed", fileName, timestamp, level, clientId, message);
            _consoleOutput.WriteError($"[{timestamp}]", $"[ClientClosed] [Client: {clientId}] => {message}");
            logEvent = new { Type = "ClientClosed", Timestamp = timestamp, Level = level, ClientId = clientId, Message = message, FileName = fileName };
        }
        else if (message.Contains("Successfully connected to stdio server", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("{EventType} detected: {LogFile} {Timestamp} [{Level}] {ClientId}: {Message}", 
                "Connected", fileName, timestamp, level, clientId, message);
            _consoleOutput.WriteSuccess($"[{timestamp}]", $"[Connected] [Client: {clientId}] => {message}");
            logEvent = new { Type = "Connected", Timestamp = timestamp, Level = level, ClientId = clientId, Message = message, FileName = fileName };
        }
        else
        {
            // Default handling based on log level
            if (level.Equals("error", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("{EventType} detected: {LogFile} {Timestamp} [{Level}] {ClientId}: {Message}", 
                    "GenericError", fileName, timestamp, level, clientId, message);
                _consoleOutput.WriteError($"[{timestamp}]", $"[{level}] [Client: {clientId}] => {message}");
                logEvent = new { Type = "GenericError", Timestamp = timestamp, Level = level, ClientId = clientId, Message = message, FileName = fileName };
            }
            else if (level.Equals("warning", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("{EventType} detected: {LogFile} {Timestamp} [{Level}] {ClientId}: {Message}", 
                    "GenericWarning", fileName, timestamp, level, clientId, message);
                _consoleOutput.WriteWarning($"[{timestamp}]", $"[{level}] [Client: {clientId}] => {message}");
                logEvent = new { Type = "GenericWarning", Timestamp = timestamp, Level = level, ClientId = clientId, Message = message, FileName = fileName };
            }
            else
            {
                _logger.LogInformation("{EventType} detected: {LogFile} {Timestamp} [{Level}] {ClientId}: {Message}", 
                    "GenericInfo", fileName, timestamp, level, clientId, message);
                _consoleOutput.WriteInfo($"[{timestamp}]", $"[{level}] [Client: {clientId}] => {message}");
                logEvent = new { Type = "GenericInfo", Timestamp = timestamp, Level = level, ClientId = clientId, Message = message, FileName = fileName };
            }
        }

        _webSocketService.BroadcastAsync(logEvent).ConfigureAwait(false);
    }
} 