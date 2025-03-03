namespace CursorMCPMonitor.Services;

/// <summary>
/// Interface for console output formatting and display.
/// </summary>
public interface IConsoleOutputService
{
    /// <summary>
    /// Writes a raw message to the console.
    /// </summary>
    /// <param name="prefix">The prefix to display before the message</param>
    /// <param name="message">The message to display</param>
    void WriteRaw(string prefix, string message);

    /// <summary>
    /// Writes an informational message to the console.
    /// </summary>
    /// <param name="prefix">The prefix to display before the message</param>
    /// <param name="message">The message to display</param>
    void WriteInfo(string prefix, string message);

    /// <summary>
    /// Writes a success message to the console.
    /// </summary>
    /// <param name="prefix">The prefix to display before the message</param>
    /// <param name="message">The message to display</param>
    void WriteSuccess(string prefix, string message);

    /// <summary>
    /// Writes a warning message to the console.
    /// </summary>
    /// <param name="prefix">The prefix to display before the message</param>
    /// <param name="message">The message to display</param>
    void WriteWarning(string prefix, string message);

    /// <summary>
    /// Writes an error message to the console.
    /// </summary>
    /// <param name="prefix">The prefix to display before the message</param>
    /// <param name="message">The message to display</param>
    void WriteError(string prefix, string message);

    /// <summary>
    /// Writes a highlighted message to the console.
    /// </summary>
    /// <param name="prefix">The prefix to display before the message</param>
    /// <param name="message">The message to display</param>
    void WriteHighlight(string prefix, string message);
} 