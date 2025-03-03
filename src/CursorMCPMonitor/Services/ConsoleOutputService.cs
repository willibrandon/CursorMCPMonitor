using System.Text;
using CursorMCPMonitor.Interfaces;
using Microsoft.Extensions.Logging;

namespace CursorMCPMonitor.Services;

/// <summary>
/// Service for handling console output formatting and display.
/// </summary>
public class ConsoleOutputService : IConsoleOutputService
{
    private readonly ILogger<ConsoleOutputService> _logger;
    private static readonly object _consoleLock = new();

    public ConsoleOutputService(ILogger<ConsoleOutputService> logger)
    {
        _logger = logger;
        Console.OutputEncoding = Encoding.UTF8;
    }

    /// <summary>
    /// Writes a raw message to the console.
    /// </summary>
    public void WriteRaw(string prefix, string message)
    {
        lock (_consoleLock)
        {
            Console.ResetColor();
            Console.Write($"{prefix} ");
            Console.WriteLine(message);
            Console.ResetColor();
        }
        
        // Log with structured properties
        _logger.LogInformation("{OutputType} {Prefix} {Message}", "Raw", prefix, message);
    }

    /// <summary>
    /// Writes an informational message to the console.
    /// </summary>
    public void WriteInfo(string prefix, string message)
    {
        lock (_consoleLock)
        {
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($"{prefix} ");
            Console.WriteLine(message);
            Console.ResetColor();
        }
        
        // Log with structured properties
        _logger.LogInformation("{OutputType} {Prefix} {Message}", "Info", prefix, message);
    }

    /// <summary>
    /// Writes a success message to the console.
    /// </summary>
    public void WriteSuccess(string prefix, string message)
    {
        lock (_consoleLock)
        {
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{prefix} ");
            Console.WriteLine(message);
            Console.ResetColor();
        }
        
        // Log with structured properties
        _logger.LogInformation("{OutputType} {Prefix} {Message}", "Success", prefix, message);
    }

    /// <summary>
    /// Writes a warning message to the console.
    /// </summary>
    public void WriteWarning(string prefix, string message)
    {
        lock (_consoleLock)
        {
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write($"{prefix} ");
            Console.WriteLine(message);
            Console.ResetColor();
        }
        
        // Log with structured properties
        _logger.LogWarning("{OutputType} {Prefix} {Message}", "Warning", prefix, message);
    }

    /// <summary>
    /// Writes an error message to the console.
    /// </summary>
    public void WriteError(string prefix, string message)
    {
        lock (_consoleLock)
        {
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"{prefix} ");
            Console.WriteLine(message);
            Console.ResetColor();
        }
        
        // Log with structured properties
        _logger.LogError("{OutputType} {Prefix} {Message}", "Error", prefix, message);
    }

    /// <summary>
    /// Writes a highlighted message to the console.
    /// </summary>
    public void WriteHighlight(string prefix, string message)
    {
        lock (_consoleLock)
        {
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{prefix} ");
            Console.WriteLine(message);
            Console.ResetColor();
        }
        
        // Log with structured properties
        _logger.LogInformation("{OutputType} {Prefix} {Message}", "Highlight", prefix, message);
    }
} 