using Microsoft.Extensions.Configuration;

namespace CursorMCPMonitor.Configuration;

/// <summary>
/// Represents the application configuration settings.
/// </summary>
public class AppConfig
{
    /// <summary>
    /// Gets or sets the root directory for Cursor logs.
    /// </summary>
    public string? LogsRoot { get; set; }
    
    /// <summary>
    /// Gets or sets the polling interval in milliseconds.
    /// </summary>
    public int PollIntervalMs { get; set; } = 1000;
    
    /// <summary>
    /// Gets or sets the log file pattern to monitor.
    /// </summary>
    public string LogPattern { get; set; } = "Cursor MCP.log";
    
    /// <summary>
    /// Gets or sets the log verbosity level.
    /// </summary>
    public string Verbosity { get; set; } = "Information";
    
    /// <summary>
    /// Gets the default logs directory based on the operating system.
    /// </summary>
    /// <returns>The default logs directory path</returns>
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
        
        // If LogsRoot is null, use the default
        config.LogsRoot ??= GetDefaultLogsDirectory();
        
        // Map logging level from Logging:LogLevel:Default if set
        var loggingLevel = configuration.GetValue<string>("Logging:LogLevel:Default");
        if (!string.IsNullOrEmpty(loggingLevel))
        {
            config.Verbosity = loggingLevel;
        }
        
        return config;
    }
} 