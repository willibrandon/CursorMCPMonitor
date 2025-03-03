using System.Text;

namespace CursorMCPMonitor;

/// <summary>
/// A simple class that tails a file line-by-line in the background.
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

    public LogTailer(string filePath, Action<string, string> onLine, int pollIntervalMs = 1000)
    {
        _filePath = filePath;
        _onLine = onLine;
        _pollIntervalMs = pollIntervalMs;
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
        while (!_stop)
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    // File doesn't exist (yet or anymore), reset position
                    _lastPosition = 0;
                    _isFirstRead = true;
                    Thread.Sleep(_pollIntervalMs);
                    continue;
                }

                using var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                
                // Handle file truncation or rotation
                if (fs.Length < _lastPosition || (_lastPosition == 0 && !_isFirstRead))
                {
                    // File was truncated or rotated, start from beginning
                    _lastPosition = 0;
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

                _isFirstRead = false;
            }
            catch (Exception ex)
            {
                // If the file is locked or has an IO error, etc. 
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[LogTailer Error] on {Path.GetFileName(_filePath)}: {ex.Message}");
                Console.ResetColor();
                
                // Reset state on error
                _lastPosition = 0;
                _isFirstRead = true;
            }

            Thread.Sleep(_pollIntervalMs);
        }
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
} 