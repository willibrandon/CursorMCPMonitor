using CursorMCPMonitor.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using CursorMCPMonitor.Configuration;

namespace CursorMCPMonitor.Services;

/// <summary>
/// Service for processing log lines from Cursor MCP log files.
/// Handles parsing structured log lines and special case handling.
/// </summary>
public partial class LogProcessorService : ILogProcessorService
{
    private readonly IConsoleOutputService _consoleOutput;
    private readonly ILogger<LogProcessorService> _logger;

    // Regex to parse lines of the form:
    // 2025-03-02 12:26:34.698 [info] a602: Handling CreateClient action
    [GeneratedRegex(@"^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}) \[(?<level>\w+)\]\s+(?<clientId>\w+):\s+(?<message>.*)$", RegexOptions.Compiled | RegexOptions.ExplicitCapture)]
    private static partial Regex LogLineRegex();

    public LogProcessorService(
        IConsoleOutputService consoleOutput,
        ILogger<LogProcessorService> logger)
    {
        _consoleOutput = consoleOutput;
        _logger = logger;
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
        
        // Handle unstructured lines
        if (line.Contains("No server info found", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError("NoServerInfo from {LogFile}: {Message}", fileName, line);
            _consoleOutput.WriteError("[NoServerInfo]", line);
        }
        else if (line.Contains("unrecognized_keys", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError("UnrecognizedKeys from {LogFile}: {Message}", fileName, line);
            _consoleOutput.WriteError("[UnrecognizedKeys]", line);
        }
        else if (line.Contains("No workspace folders found", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("NoWorkspace from {LogFile}: {Message}", fileName, line);
            _consoleOutput.WriteWarning("[Warning]", line);
        }
        else
        {
            _logger.LogInformation("RawLine from {LogFile}: {Message}", fileName, line);
            _consoleOutput.WriteRaw("[Raw]", line);
        }
    }

    /// <summary>
    /// Processes a structured log line (matching the standard log format).
    /// </summary>
    private void ProcessStructuredLine(string fullFilePath, string timestamp, string level, string clientId, string message)
    {
        var fileName = Path.GetFileName(fullFilePath);
        var logProperties = new
        {
            LogFile = fileName,
            Timestamp = timestamp,
            Level = level,
            ClientId = clientId,
            Message = message
        };
        
        // Process different message types
        if (message.Contains("CreateClient action", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("{EventType} detected: {LogFile} {Timestamp} [{Level}] {ClientId}: {Message}", 
                "CreateClient", fileName, timestamp, level, clientId, message);
            _consoleOutput.WriteSuccess($"[{timestamp}]", $"[CreateClient] [Client: {clientId}] => {message}");
        }
        else if (message.Contains("ListOfferings action", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("{EventType} detected: {LogFile} {Timestamp} [{Level}] {ClientId}: {Message}", 
                "ListOfferings", fileName, timestamp, level, clientId, message);
            _consoleOutput.WriteHighlight($"[{timestamp}]", $"[ListOfferings] [Client: {clientId}] => {message}");
        }
        else if (message.Contains("Error in MCP:", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError("{EventType} detected: {LogFile} {Timestamp} [{Level}] {ClientId}: {Message}", 
                "MCPError", fileName, timestamp, level, clientId, message);
            _consoleOutput.WriteError($"[{timestamp}]", $"[Error] [Client: {clientId}] => {message}");
        }
        else if (message.Contains("Client closed for command", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("{EventType} detected: {LogFile} {Timestamp} [{Level}] {ClientId}: {Message}", 
                "ClientClosed", fileName, timestamp, level, clientId, message);
            _consoleOutput.WriteError($"[{timestamp}]", $"[ClientClosed] [Client: {clientId}] => {message}");
        }
        else if (message.Contains("Successfully connected to stdio server", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("{EventType} detected: {LogFile} {Timestamp} [{Level}] {ClientId}: {Message}", 
                "Connected", fileName, timestamp, level, clientId, message);
            _consoleOutput.WriteSuccess($"[{timestamp}]", $"[Connected] [Client: {clientId}] => {message}");
        }
        else
        {
            // Default handling based on log level
            if (level.Equals("error", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("{EventType} detected: {LogFile} {Timestamp} [{Level}] {ClientId}: {Message}", 
                    "GenericError", fileName, timestamp, level, clientId, message);
                _consoleOutput.WriteError($"[{timestamp}]", $"[{level}] [Client: {clientId}] => {message}");
            }
            else if (level.Equals("warning", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("{EventType} detected: {LogFile} {Timestamp} [{Level}] {ClientId}: {Message}", 
                    "GenericWarning", fileName, timestamp, level, clientId, message);
                _consoleOutput.WriteWarning($"[{timestamp}]", $"[{level}] [Client: {clientId}] => {message}");
            }
            else
            {
                _logger.LogInformation("{EventType} detected: {LogFile} {Timestamp} [{Level}] {ClientId}: {Message}", 
                    "GenericInfo", fileName, timestamp, level, clientId, message);
                _consoleOutput.WriteInfo($"[{timestamp}]", $"[{level}] [Client: {clientId}] => {message}");
            }
        }
    }
} 