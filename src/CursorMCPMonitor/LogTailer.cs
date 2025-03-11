using System.Text;

namespace CursorMCPMonitor;

/// <summary>
/// A class that monitors a log file for changes and processes new lines as they are added.
/// Supports file rotation, truncation, and real-time updates.
/// </summary>
public class LogTailer : IDisposable
{
    private readonly string _filePath;
    private readonly Action<string, string> _onLine;
    private readonly Thread _thread;
    private bool _stop;
    private long _lastPosition;
    private readonly int _pollIntervalMs;
    private bool _isFirstRead = true;
    private int _consecutiveErrorCount = 0;
    private readonly int _maxRetries = 5;
    private const int MAX_BACKOFF_MS = 10000; // 10 seconds maximum backoff
    private DateTime _lastTruncationMessage = DateTime.MinValue;
    private const int TRUNCATION_MESSAGE_THROTTLE_MS = 5000; // Only show truncation message every 5 seconds

    /// <summary>
    /// Initializes a new instance of the LogTailer class.
    /// </summary>
    /// <param name="filePath">The full path to the log file to monitor.</param>
    /// <param name="onLine">Callback action that is invoked for each new line detected. Takes file path and line content as parameters.</param>
    /// <param name="pollIntervalMs">Optional interval in milliseconds between checks for new content. Defaults to 1000ms.</param>
    public LogTailer(string filePath, Action<string, string> onLine, int pollIntervalMs = 1000)
    {
        _filePath = filePath;
        _onLine = onLine;
        _pollIntervalMs = pollIntervalMs;
        _thread = new Thread(Run) { IsBackground = true };
        _thread.Start();
    }

    /// <summary>
    /// Stops the log tailing operation and cleans up resources.
    /// </summary>
    public void Stop()
    {
        _stop = true;
        try
        {
            _thread.Join(2000);
        }
        catch { /* ignore */ }
    }

    /// <summary>
    /// Calculates exponential backoff time based on consecutive error count.
    /// </summary>
    /// <returns>Time to wait in milliseconds</returns>
    private int CalculateBackoff()
    {
        // Exponential backoff with jitter: 2^n * (0.5 to 1.5) milliseconds
        int baseBackoff = Math.Min((int)Math.Pow(2, _consecutiveErrorCount) * 100, MAX_BACKOFF_MS);
        Random random = new Random();
        double jitter = 0.5 + random.NextDouble();
        return (int)(baseBackoff * jitter);
    }

    private void Run()
    {
        bool reportedTruncation = false;
        long previousFileSize = -1;

        while (!_stop)
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    // File doesn't exist (yet or anymore), reset position
                    _lastPosition = 0;
                    _isFirstRead = true;
                    previousFileSize = -1;
                    reportedTruncation = false;
                    Thread.Sleep(_pollIntervalMs);
                    continue;
                }

                // Get file info before opening to check if size is different
                var fileInfo = new FileInfo(_filePath);
                long currentSize = fileInfo.Length;

                using var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                
                // Handle file truncation or rotation - only if we've actually seen this file before
                // and the file size has decreased since last check
                bool fileWasTruncated = !_isFirstRead && previousFileSize >= 0 && currentSize < previousFileSize;
                
                // Also detect if we've already read past the end of the file (which shouldn't happen normally)
                bool positionIsPastEnd = _lastPosition > fs.Length;
                
                if (fileWasTruncated || positionIsPastEnd)
                {
                    // File was truncated or rotated, start from beginning
                    _lastPosition = 0;
                    
                    // Only show truncation message if we haven't reported it recently
                    var now = DateTime.Now;
                    if (!reportedTruncation || (now - _lastTruncationMessage).TotalMilliseconds > TRUNCATION_MESSAGE_THROTTLE_MS)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"[LogTailer] File {Path.GetFileName(_filePath)} was rotated or truncated, restarting from beginning.");
                        Console.ResetColor();
                        _lastTruncationMessage = now;
                        reportedTruncation = true;
                    }
                }
                else
                {
                    reportedTruncation = false;
                }

                if (fs.Length > _lastPosition)
                {
                    fs.Seek(_lastPosition, SeekOrigin.Begin);
                    using var reader = new StreamReader(fs, Encoding.UTF8);
                    
                    // Read the file line by line
                    string? line;
                    var buffer = new StringBuilder();
                    
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (buffer.Length > 0)
                        {
                            buffer.AppendLine(line);
                            line = buffer.ToString().TrimEnd();
                            buffer.Clear();
                        }
                        
                        _onLine(_filePath, line);
                    }

                    // Update position only after successful read
                    _lastPosition = fs.Position;
                }

                // Store the current file size for next comparison
                previousFileSize = currentSize;
                _isFirstRead = false;
                
                // Reset error count on success
                if (_consecutiveErrorCount > 0)
                {
                    _consecutiveErrorCount = 0;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[LogTailer] Recovered from previous errors on {Path.GetFileName(_filePath)}");
                    Console.ResetColor();
                }
            }
            catch (IOException ex)
            {
                _consecutiveErrorCount++;
                int backoffTime = CalculateBackoff();
                
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"[LogTailer Warning] I/O error on {Path.GetFileName(_filePath)}: {ex.Message}");
                Console.WriteLine($"[LogTailer Warning] Retrying in {backoffTime}ms (attempt {_consecutiveErrorCount} of {_maxRetries})");
                Console.ResetColor();
                
                if (_consecutiveErrorCount >= _maxRetries)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[LogTailer Error] Maximum retries ({_maxRetries}) reached for {Path.GetFileName(_filePath)}");
                    Console.WriteLine($"[LogTailer Error] Will continue to attempt monitoring but with less frequent retries");
                    Console.ResetColor();
                    _consecutiveErrorCount = _maxRetries; // Cap at max retries
                }
                
                // Reset state on error
                _lastPosition = 0;
                _isFirstRead = true;
                
                // Wait with backoff before retrying
                Thread.Sleep(backoffTime);
                continue;
            }
            catch (Exception ex)
            {
                // If we have another type of error, log it
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[LogTailer Error] Unexpected error on {Path.GetFileName(_filePath)}: {ex.Message}");
                Console.ResetColor();
                
                // Reset state on error
                _lastPosition = 0;
                _isFirstRead = true;
                
                // Wait before retrying
                Thread.Sleep(_pollIntervalMs * 2);
            }

            Thread.Sleep(_pollIntervalMs);
        }
    }

    /// <summary>
    /// Implements the IDisposable pattern to clean up resources.
    /// </summary>
    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
