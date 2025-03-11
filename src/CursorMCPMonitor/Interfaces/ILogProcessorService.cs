namespace CursorMCPMonitor.Interfaces;

/// <summary>
/// Interface for processing log entries.
/// </summary>
public interface ILogProcessorService
{
    /// <summary>
    /// Processes a log line from a specific file.
    /// </summary>
    /// <param name="filePath">Path to the log file</param>
    /// <param name="line">Log line content</param>
    void ProcessLogLine(string filePath, string line);
    
    /// <summary>
    /// Sets or updates the filter pattern to be applied to log output
    /// </summary>
    /// <param name="filterPattern">Text pattern to filter logs (can be null to disable filtering)</param>
    void SetFilter(string? filterPattern);
    
    /// <summary>
    /// Sets the minimum verbosity level to display
    /// </summary>
    /// <param name="level">Minimum log level to display (Debug, Info, Warning, Error)</param>
    void SetVerbosityLevel(string level);
}
