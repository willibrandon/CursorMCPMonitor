namespace CursorMCPMonitor.Interfaces;

/// <summary>
/// Interface for processing log lines from log files.
/// </summary>
public interface ILogProcessorService
{
    /// <summary>
    /// Processes a line from a log file.
    /// </summary>
    /// <param name="fullFilePath">The full path to the log file</param>
    /// <param name="line">The line of text from the log file</param>
    void ProcessLogLine(string fullFilePath, string line);
} 