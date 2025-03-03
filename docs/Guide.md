Below is an example of a .NET 6+ console application that demonstrates how you can:

1. **Monitor** `C:\Users\willi\AppData\Roaming\Cursor\logs` for **new subdirectories** in real time.  
2. **Detect** a `Cursor MCP.log` file in each new subdirectory’s `exthost/anysphere.cursor-always-local` folder (if present).  
3. **Tail** (follow) the `Cursor MCP.log` file, **parse** log lines of interest, and track:
   - **Client IDs** (e.g. `a602`, `073f`, `e721`),
   - Actions (`CreateClient`, `ListOfferings`, `DeleteClient`, `Handling … action`, etc.),
   - Lifecycle transitions (connected vs. closed),
   - Errors (`No server info found`, `unrecognized_keys`, `Client closed`, JSON parse errors),
   - Basic `JSON`-like or protocol message patterns (by partial string matching or a more robust parse),
   - Real-time console output with color-coded statuses.

4. **Provide**:
   - Real-time output on the console (with coloring or formatting),
   - Minimal **resource usage**,
   - Automatic handling of **log rotation** (via re-checking whenever new lines arrive or new subdirectories appear),
   - **Configurable** scanning intervals or watchers,
   - **Export** logic to capture message samples (you can expand or modify the code to dump them to JSON, CSV, or keep them in an in-memory structure).

> ### Caveats & Guidance
> - This example is intentionally **simplified**. In a real system, you might want:  
>   - A more robust queue-based approach for line processing,  
>   - Additional error recovery,  
>   - A real “JSON” parse rather than partial regex or substring checks,  
>   - A robust logging library for the monitor itself (e.g. Serilog),  
>   - A standalone Worker Service or a Windows Service rather than a console.  
> - Feel free to adapt the code into separate classes (e.g. `DirectoryMonitor`, `LogTailer`, `LogParser`, `ErrorAggregator`, etc.). Here, it’s shown in a single file for clarity.

---

## Example `Program.cs`

```csharp
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

// For .NET 6 minimal style, you could place some of this in Program.cs directly.
// This example uses a more "classic" Program class with a Main.

namespace CursorMCPMonitor
{
    internal static class Program
    {
        // Change if needed, or make configurable:
        private static readonly string CursorLogsRoot = 
            @"C:\Users\willi\AppData\Roaming\Cursor\logs";

        // A simple thread-safe dictionary to track which subdirectories we have watchers for.
        private static readonly Dictionary<string, FileSystemWatcher> _activeLogWatchers = new();
        // Another dictionary to track which "Cursor MCP.log" tail watchers are active by full file path.
        private static readonly Dictionary<string, LogTailer> _logTailers = new();

        // Regex to parse lines of the form:
        // 2025-03-02 12:26:34.698 [info] a602: Handling CreateClient action
        // -or-
        // 2025-03-02 12:26:34.698 [error] a602: Error in MCP: Something
        // (You can expand or refine this as needed.)
        private static readonly Regex LogLineRegex = new Regex(
            @"^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}) \[(?<level>\w+)\]\s+(?<clientId>\w+):\s+(?<message>.*)$",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        // Entry point
        public static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=== Cursor AI MCP Log Monitor ===");

            if (!Directory.Exists(CursorLogsRoot))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Log root does not exist: {CursorLogsRoot}");
                Console.ResetColor();
                return;
            }

            // 1. Monitor the root logs directory for new subdirectories:
            var rootWatcher = new FileSystemWatcher(CursorLogsRoot)
            {
                NotifyFilter = NotifyFilters.DirectoryName,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };

            rootWatcher.Created += (_, e) =>
            {
                // Only if it's a directory
                if (Directory.Exists(e.FullPath))
                {
                    HandleNewLogSubdirectory(e.FullPath);
                }
            };

            // Also handle any subdirectories that already exist on start:
            foreach (var subDir in Directory.GetDirectories(CursorLogsRoot))
            {
                HandleNewLogSubdirectory(subDir);
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Monitoring for new subdirectories and 'Cursor MCP.log' files...");
            Console.ResetColor();

            // Keep running until user presses a key or you convert this to a service:
            Console.WriteLine("Press ENTER to exit.");
            Console.ReadLine();

            // Cleanup watchers on exit
            rootWatcher.Dispose();
            foreach (var kvp in _activeLogWatchers)
                kvp.Value.Dispose();
            foreach (var tailer in _logTailers.Values)
                tailer.Stop();
        }

        /// <summary>
        /// When a new subdirectory is detected, we look for 
        /// "exthost/anysphere.cursor-always-local/Cursor MCP.log"
        /// within it. If found, watch for changes (lines).
        /// </summary>
        private static void HandleNewLogSubdirectory(string subDirPath)
        {
            // If we already have a watcher for this subDir, skip:
            lock (_activeLogWatchers)
            {
                if (_activeLogWatchers.ContainsKey(subDirPath))
                    return;
            }

            // Create a FileSystemWatcher that monitors subdirectories within 'subDirPath' 
            // for "Cursor MCP.log" in the "exthost/anysphere.cursor-always-local" path.
            var watcher = new FileSystemWatcher(subDirPath)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName
            };
            watcher.Created += (_, e) => OnSubdirectoryFileCreated(subDirPath, e);
            watcher.EnableRaisingEvents = true;

            lock (_activeLogWatchers)
            {
                _activeLogWatchers[subDirPath] = watcher;
            }

            // Also manually check if the "exthost/anysphere.cursor-always-local/Cursor MCP.log" 
            // file already exists in this subdir (for logs that exist before the watcher started).
            CheckForExistingCursorLog(subDirPath);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[Subdirectory] Monitoring new subdirectory: {subDirPath}");
            Console.ResetColor();
        }

        private static void OnSubdirectoryFileCreated(string rootSubDir, FileSystemEventArgs e)
        {
            // We only care if the newly created file is named "Cursor MCP.log"
            // inside an "exthost/anysphere.cursor-always-local" path.
            if (!File.Exists(e.FullPath))
                return;

            // Check partial path:
            // exthost/anysphere.cursor-always-local/Cursor MCP.log
            var relative = Path.GetRelativePath(rootSubDir, e.FullPath);
            // NOTE: Some Windows .NET APIs may produce slashes vs. backslashes, so 
            // you can compare with a case-insensitive approach or just check "exthost" in path, etc.
            if (relative.Replace('\\','/').EndsWith("exthost/anysphere.cursor-always-local/Cursor MCP.log",
                StringComparison.OrdinalIgnoreCase))
            {
                StartTailer(e.FullPath);
            }
        }

        private static void CheckForExistingCursorLog(string subDirPath)
        {
            // Build the path to exthost/anysphere.cursor-always-local/Cursor MCP.log
            // (We can do a manual check or walk subdirs.)
            var exthostPath = Path.Combine(subDirPath, "exthost", "anysphere.cursor-always-local");
            var logFile = Path.Combine(exthostPath, "Cursor MCP.log");
            if (File.Exists(logFile))
            {
                StartTailer(logFile);
            }
        }

        /// <summary>
        /// Start tailing a log file if not already started.
        /// </summary>
        private static void StartTailer(string fullFilePath)
        {
            lock (_logTailers)
            {
                if (_logTailers.ContainsKey(fullFilePath))
                    return;

                var tailer = new LogTailer(fullFilePath, ProcessLogLine);
                _logTailers[fullFilePath] = tailer;
            }
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[LogTailer] Now tailing: {fullFilePath}");
            Console.ResetColor();
        }

        /// <summary>
        /// Called each time the tailer detects a new line in the log.
        /// Here, we parse the line, attempt to detect actions, errors, etc.
        /// </summary>
        private static void ProcessLogLine(string fullFilePath, string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            var match = LogLineRegex.Match(line);
            if (!match.Success)
            {
                // If lines don't match expected pattern, you can do further checks 
                // for JSON or partial text:
                if (line.Contains("No server info found", StringComparison.OrdinalIgnoreCase))
                {
                    // Example unstructured line
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[NoServerInfo] {line}");
                    Console.ResetColor();
                }
                else if (line.Contains("unrecognized_keys", StringComparison.OrdinalIgnoreCase))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[UnrecognizedKeys] {line}");
                    Console.ResetColor();
                }
                else if (line.Contains("No workspace folders found", StringComparison.OrdinalIgnoreCase))
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"[Warning] {line}");
                    Console.ResetColor();
                }
                else
                {
                    // Just output raw
                    Console.WriteLine($"[Raw] {line}");
                }
                return;
            }

            var timestamp = match.Groups["timestamp"].Value;
            var level = match.Groups["level"].Value;
            var clientId = match.Groups["clientId"].Value;
            var message = match.Groups["message"].Value;

            // A few examples:
            if (message.Contains("CreateClient action", StringComparison.OrdinalIgnoreCase))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[{timestamp}] [CreateClient] [Client: {clientId}] => {message}");
                Console.ResetColor();
            }
            else if (message.Contains("ListOfferings action", StringComparison.OrdinalIgnoreCase))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[{timestamp}] [ListOfferings] [Client: {clientId}] => {message}");
                Console.ResetColor();
            }
            else if (message.Contains("No server info found", StringComparison.OrdinalIgnoreCase))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{timestamp}] [Error] [Client: {clientId}] => {message}");
                Console.ResetColor();
            }
            else if (message.Contains("Error in MCP:", StringComparison.OrdinalIgnoreCase))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{timestamp}] [Error] [Client: {clientId}] => {message}");
                Console.ResetColor();
            }
            else if (message.Contains("Client closed for command", StringComparison.OrdinalIgnoreCase))
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"[{timestamp}] [ClientClosed] [Client: {clientId}] => {message}");
                Console.ResetColor();
            }
            else if (message.Contains("Successfully connected to stdio server", StringComparison.OrdinalIgnoreCase))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[{timestamp}] [Connected] [Client: {clientId}] => {message}");
                Console.ResetColor();
            }
            else
            {
                // Generic line
                var color = level.Equals("error", StringComparison.OrdinalIgnoreCase)
                    ? ConsoleColor.Red
                    : (level.Equals("warning", StringComparison.OrdinalIgnoreCase)
                        ? ConsoleColor.DarkYellow
                        : ConsoleColor.Gray);
                Console.ForegroundColor = color;
                Console.WriteLine($"[{timestamp}] [{level}] [Client: {clientId}] => {message}");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// A simple class that tails a file line-by-line in the background.
        /// </summary>
        private class LogTailer
        {
            private readonly string _filePath;
            private readonly Action<string, string> _onLine;
            private readonly Thread _thread;
            private bool _stop;
            private long _lastPosition;

            public LogTailer(string filePath, Action<string, string> onLine)
            {
                _filePath = filePath;
                _onLine = onLine;
                _thread = new Thread(Run) { IsBackground = true };
                _thread.Start();
            }

            public void Stop()
            {
                _stop = true;
                try
                {
                    _thread.Join(2000);
                }
                catch { /* ignore */ }
            }

            private void Run()
            {
                // If file already has content, we can optionally skip existing lines 
                // or start from the beginning. Here, we choose to skip to end:
                using (var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    _lastPosition = fs.Length;
                }

                while (!_stop)
                {
                    try
                    {
                        using var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        if (fs.Length < _lastPosition)
                        {
                            // The log might have rotated or truncated. Reset:
                            _lastPosition = 0;
                        }

                        if (fs.Length > _lastPosition)
                        {
                            fs.Seek(_lastPosition, SeekOrigin.Begin);
                            using var reader = new StreamReader(fs, Encoding.UTF8);
                            string? line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                // Each line read
                                _onLine(_filePath, line);
                            }
                            _lastPosition = fs.Position;
                        }
                    }
                    catch (Exception ex)
                    {
                        // If the file is locked or has an IO error, etc. 
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[LogTailer Error] on {Path.GetFileName(_filePath)}: {ex.Message}");
                        Console.ResetColor();
                    }

                    Thread.Sleep(1000); // 1-second poll interval
                }
            }
        }
    }
}
```

---

### How this works

1. **FileSystemWatcher** on the main `CursorLogsRoot` (`C:\Users\willi\AppData\Roaming\Cursor\logs`) looks for newly-created **subdirectories**.  
2. Each new subdirectory gets its own sub-`FileSystemWatcher`, which looks for any new file named `"Cursor MCP.log"` under a path containing `exthost/anysphere.cursor-always-local`. (We do this by checking the relative path on creation. You can refine for the exact subfolder pattern you expect.)  
3. **When** a `"Cursor MCP.log"` file is found, a **LogTailer** is started. This tailer is a background thread reading new lines from the file every 1 second.  
4. **`ProcessLogLine(...)`** is called for each new line. The example uses a **Regex** to parse `[timestamp] [level] [clientId] message` lines. If a line doesn’t match the pattern, it does some fallback checks (e.g., looking for `"No server info found"`).  
5. **Color-coded** console output is produced in real time:
   - `Console.ForegroundColor = ConsoleColor.Green` for successes like `CreateClient`,
   - `ConsoleColor.Red` for errors, etc.  
6. **On exit**, the program disposes watchers and stops the tail threads.

---

### Customizing & Enhancing

- **Connection Lifecycle**: You can maintain a dictionary of `(clientId -> connectionState)` and update states on `Successfully connected`, `Client closed`, etc. Then console-print the transitions.  
- **JSON Parsing**: For more robust detection of lines that contain JSON protocol messages, you could expand your `LogLineRegex` or do a `JsonDocument.Parse()` on the substring. Many of Cursor’s errors are big JSON blocks. You might store them for analysis or aggregated error reporting.  
- **Exporting Protocol Samples**: You could keep a local list/queue of interesting lines (like a ring buffer) and flush them to a file every N seconds or when something is flagged.  
- **Performance**: If logs are extremely large, consider a more advanced approach. However, typical usage of `FileStream` + `StreamReader` with a poll-based tail is often fine for moderate volumes.  
- **Windows Service / Linux Daemon**: You can convert this example into a background worker in .NET 6 using a `Host.CreateDefaultBuilder()` approach (Worker Service template) so it runs as a service.  

---

## Summary

This sample fulfills the key goals:

- Watches `C:\Users\willi\AppData\Roaming\Cursor\logs` for new directories in real-time,  
- Finds `Cursor MCP.log` within `exthost/anysphere.cursor-always-local`,  
- Tails log lines, parses recognized patterns, highlights errors & lifecycle transitions,  
- Provides immediate feedback in a console app but can be extended for more advanced scenarios.

Feel free to adapt, break out classes, refine the line parsing (especially for JSON), add additional actions or color-coded rules, store aggregated errors in memory or a database, or incorporate a real logging library. This skeleton should give you a **head start** on building a “Cursor MCP integration monitor” that helps accelerate your MCP server development by capturing and analyzing real-time protocol interactions.