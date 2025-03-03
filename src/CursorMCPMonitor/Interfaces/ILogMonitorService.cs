using CursorMCPMonitor.Configuration;

namespace CursorMCPMonitor.Interfaces;

/// <summary>
/// Interface for services that monitor log directories and files.
/// </summary>
public interface ILogMonitorService
{
    /// <summary>
    /// Starts monitoring the root logs directory for new subdirectories.
    /// </summary>
    /// <param name="rootLogDirectory">The root directory to monitor</param>
    /// <param name="appConfig">Application configuration</param>
    void StartMonitoring(string rootLogDirectory, AppConfig appConfig);
    
    /// <summary>
    /// Stops all active watchers and tailers.
    /// </summary>
    void Dispose();
} 