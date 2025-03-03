using Microsoft.Extensions.Configuration;

namespace CursorMCPMonitor.Configuration;

/// <summary>
/// Application configuration settings.
/// </summary>
public class AppConfig
{
    /// <summary>
    /// Root directory where Cursor log files are stored.
    /// </summary>
    public string? LogsRoot { get; set; }
    
    /// <summary>
    /// Interval in milliseconds between checks for new content.
    /// </summary>
    public int PollIntervalMs { get; set; } = 1000;
    
    /// <summary>
    /// The pattern to match log files against.
    /// Supports glob patterns like "Cursor MCP*.log"
    /// </summary>
    public string LogPattern { get; set; } = "Cursor MCP.log";
    
    /// <summary>
    /// The verbosity level for log output.
    /// </summary>
    public string Verbosity { get; set; } = "Debug";
    
    /// <summary>
    /// Optional text pattern to filter log content.
    /// Only lines containing this text will be displayed.
    /// </summary>
    public string? Filter { get; set; }
    
    /// <summary>
    /// Gets the default logs directory for Cursor.
    /// </summary>
    public static string GetDefaultLogsDirectory() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Cursor",
        "logs"
    );
    
    /// <summary>
    /// Loads configuration from an IConfiguration instance.
    /// </summary>
    /// <param name="configuration">The configuration source</param>
    /// <returns>An initialized AppConfig instance</returns>
    public static AppConfig Load(IConfiguration configuration)
    {
        var config = new AppConfig();
        configuration.Bind(config);
        
        // If LogsRoot is null or empty, use the default
        if (string.IsNullOrEmpty(config.LogsRoot))
        {
            config.LogsRoot = GetDefaultLogsDirectory();
        }
        
        // Map logging level from Logging:LogLevel:Default if set
        var loggingLevel = configuration.GetValue<string>("Logging:LogLevel:Default");
        if (!string.IsNullOrEmpty(loggingLevel))
        {
            config.Verbosity = loggingLevel;
        }
        
        return config;
    }
} 