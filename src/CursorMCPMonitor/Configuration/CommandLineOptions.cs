using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

namespace CursorMCPMonitor.Configuration;

/// <summary>
/// Defines command-line options for the application and binds them to configuration.
/// </summary>
public class CommandLineOptions
{
    /// <summary>
    /// Configures command-line options and adds them to the host configuration.
    /// </summary>
    /// <param name="args">Command-line arguments</param>
    /// <returns>A dictionary of configuration values from command line</returns>
    public static Dictionary<string, string?> Parse(string[] args)
    {
        var options = new Dictionary<string, string?>();
        
        // Define root command
        var rootCommand = new RootCommand("Cursor MCP Monitor - Real-time monitoring of Model Context Protocol interactions")
        {
            TreatUnmatchedTokensAsErrors = true
        };
        
        // Add options
        var logsRootOption = new Option<string>(
            aliases: new[] { "--logs-root", "-l" },
            description: "Root directory containing Cursor logs");
            
        var pollIntervalOption = new Option<int>(
            aliases: new[] { "--poll-interval", "-p" },
            description: "Polling interval in milliseconds");
            
        var verbosityOption = new Option<string>(
            aliases: new[] { "--verbosity", "-v" },
            description: "Log verbosity level (debug, info, warning, error)");
            
        var logPatternOption = new Option<string>(
            aliases: new[] { "--log-pattern", "-f" },
            description: "Log file pattern to monitor");
            
        var filterOption = new Option<string>(
            aliases: new[] { "--filter" },
            description: "Filter log messages containing specific text");
            
        // Add options to root command
        rootCommand.AddOption(logsRootOption);
        rootCommand.AddOption(pollIntervalOption);
        rootCommand.AddOption(verbosityOption);
        rootCommand.AddOption(logPatternOption);
        rootCommand.AddOption(filterOption);
        
        // Set handler
        rootCommand.Handler = CommandHandler.Create<string?, int?, string?, string?, string?>(
            (logsRoot, pollInterval, verbosity, logPattern, filter) => 
            {
                if (logsRoot != null) options["LogsRoot"] = logsRoot;
                if (pollInterval != null) options["PollIntervalMs"] = pollInterval.ToString();
                if (verbosity != null) options["Verbosity"] = verbosity;
                if (logPattern != null) options["LogPattern"] = logPattern;
                if (filter != null) options["Filter"] = filter;
                
                return 0;
            });
            
        // Parse and return
        rootCommand.Invoke(args);
        return options;
    }
} 