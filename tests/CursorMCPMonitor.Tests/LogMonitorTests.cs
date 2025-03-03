using CursorMCPMonitor.Configuration;
using CursorMCPMonitor.Interfaces;
using CursorMCPMonitor.Services;
using Microsoft.Extensions.Logging;

namespace CursorMCPMonitor.Tests;

public class LogMonitorTests
{
    private readonly Mock<ILogger<LogMonitorService>> _loggerMock;
    private readonly Mock<ILogProcessorService> _processorMock;
    private readonly Mock<IConsoleOutputService> _consoleOutputMock;
    private readonly LogMonitorService _monitor;
    private readonly AppConfig _config;

    public LogMonitorTests()
    {
        _loggerMock = new Mock<ILogger<LogMonitorService>>();
        _processorMock = new Mock<ILogProcessorService>();
        _consoleOutputMock = new Mock<IConsoleOutputService>();
        _monitor = new LogMonitorService(_processorMock.Object, _loggerMock.Object, _consoleOutputMock.Object);
        _config = new AppConfig 
        { 
            LogPattern = "Cursor MCP.log",
            PollIntervalMs = 1000
        };
    }

    [Theory]
    [InlineData("")]
    public void Should_Handle_Invalid_Root_Directory(string rootDir)
    {
        // Act
        _monitor.StartMonitoring(rootDir, _config);

        // Assert
        _consoleOutputMock.Verify(x => x.WriteError("Error:", "Log root directory path is null or empty"), Times.Once);
    }

    [Fact]
    public void Should_Handle_Null_Root_Directory()
    {
        // Act
        _monitor.StartMonitoring(null!, _config);

        // Assert
        _consoleOutputMock.Verify(x => x.WriteError("Error:", "Log root directory path is null or empty"), Times.Once);
    }

    [Fact]
    public void Should_Handle_NonExistent_Directory()
    {
        // Arrange
        var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        _monitor.StartMonitoring(nonExistentDir, _config);

        // Assert
        _consoleOutputMock.Verify(x => x.WriteError("Error:", $"Log root does not exist: {nonExistentDir}"), Times.Once);
    }

    [Theory]
    [InlineData("Cursor MCP.log", true)]
    [InlineData("random.txt", false)]
    [InlineData("Cursor MCP1.log", true)]
    public void Should_Match_Log_Pattern_Correctly(string fileName, bool shouldMatch)
    {
        // Arrange
        var rootDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(rootDir);

        // Act
        _monitor.StartMonitoring(rootDir, _config);

        // Give the monitor time to set up watchers
        Thread.Sleep(500);

        // Create the log file after the monitor is running
        var tempDir = Path.Combine(rootDir, "window1", "exthost", "anysphere.cursor-always-local");
        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, fileName);
        File.WriteAllText(filePath, "test content");

        // Give the file system watcher time to process
        Thread.Sleep(500);

        // Assert
        if (shouldMatch)
        {
            _consoleOutputMock.Verify(x => x.WriteSuccess("LogTailer:", It.Is<string>(s => s.Contains(filePath))), Times.Once);
        }
        else
        {
            _consoleOutputMock.Verify(x => x.WriteSuccess("LogTailer:", It.Is<string>(s => s.Contains(filePath))), Times.Never);
        }

        try
        {
            if (Directory.Exists(rootDir))
            {
                // Give processes time to release handles
                Thread.Sleep(100);
                Directory.Delete(rootDir, true);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Ignore cleanup errors
        }
        catch (IOException)
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public void Should_Monitor_New_Subdirectories()
    {
        // Arrange
        var rootDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(rootDir);

        try
        {
            // Act
            _monitor.StartMonitoring(rootDir, _config);
            var newSubDir = Path.Combine(rootDir, "window1", "exthost", "anysphere.cursor-always-local");
            Directory.CreateDirectory(newSubDir);

            // Assert
            _consoleOutputMock.Verify(x => x.WriteSuccess("Subdirectory:", It.Is<string>(s => s.Contains(rootDir))), Times.Once);
        }
        finally
        {
            // Cleanup
            Directory.Delete(rootDir, true);
        }
    }

    [Fact]
    public void Should_Start_Tailer_For_New_Log_Files()
    {
        // Arrange
        var rootDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(rootDir);

        // Act
        _monitor.StartMonitoring(rootDir, _config);
        
        // Give the monitor time to set up watchers
        Thread.Sleep(500);
        
        // Create the log file after the monitor is running
        var logDir = Path.Combine(rootDir, "window1", "exthost", "anysphere.cursor-always-local");
        Directory.CreateDirectory(logDir);
        var logFile = Path.Combine(logDir, "Cursor MCP.log");
        File.WriteAllText(logFile, "test log content");
        
        // Give the file system watcher time to process
        Thread.Sleep(500);

        // Assert
        _consoleOutputMock.Verify(x => x.WriteSuccess("LogTailer:", It.Is<string>(s => s.Contains(logFile))), Times.Once);

        try
        {
            if (Directory.Exists(rootDir))
            {
                // Give processes time to release handles
                Thread.Sleep(100);
                Directory.Delete(rootDir, true);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Ignore cleanup errors
        }
        catch (IOException)
        {
            // Ignore cleanup errors
        }
    }
} 