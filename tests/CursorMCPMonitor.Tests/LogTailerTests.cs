namespace CursorMCPMonitor.Tests;

/// <summary>
/// Tests for the LogTailer class to verify log file monitoring functionality.
/// </summary>
public class LogTailerTests : IDisposable
{
    private readonly string _testLogFile;
    private readonly List<string> _processedLines;

    /// <summary>
    /// Initializes a new instance of the LogTailerTests class.
    /// Sets up a temporary test file path for each test.
    /// </summary>
    public LogTailerTests()
    {
        _testLogFile = Path.Combine(Path.GetTempPath(), $"test_log_{Guid.NewGuid()}.log");
        _processedLines = new List<string>();
        File.WriteAllText(_testLogFile, ""); // Create empty file
    }

    [Fact]
    public async Task Should_Process_New_Lines()
    {
        // Arrange
        var lines = new[] { "Line 1", "Line 2", "Line 3" };
        var tailer = new LogTailer(_testLogFile, ProcessLine, 100);

        // Act
        await File.WriteAllLinesAsync(_testLogFile, lines);
        await Task.Delay(1000); // Give more time for processing

        // Assert
        Assert.Equal(lines.Length, _processedLines.Count);
        Assert.Equal(lines, _processedLines);
    }

    [Fact]
    public async Task Should_Handle_File_Rotation()
    {
        // Arrange
        var initialLines = new[] { "Line 1", "Line 2" };
        var newLines = new[] { "Line 3", "Line 4" };
        var tailer = new LogTailer(_testLogFile, ProcessLine, 100);

        // Act - Write initial lines
        await File.WriteAllLinesAsync(_testLogFile, initialLines);
        await Task.Delay(1000); // Give more time for processing

        // Act - Simulate log rotation
        File.Delete(_testLogFile);
        await Task.Delay(200); // Give time for deletion to be detected
        await File.WriteAllLinesAsync(_testLogFile, newLines);
        await Task.Delay(1000); // Give more time for processing

        // Assert
        Assert.Equal(4, _processedLines.Count);
        Assert.Equal(initialLines.Concat(newLines), _processedLines);
    }

    [Fact]
    public async Task Should_Handle_File_Truncation()
    {
        // Arrange
        var initialLines = new[] { "Line 1", "Line 2" };
        var newLines = new[] { "Line 3", "Line 4" };
        var tailer = new LogTailer(_testLogFile, ProcessLine, 100);

        // Act - Write initial lines
        await File.WriteAllLinesAsync(_testLogFile, initialLines);
        await Task.Delay(1000); // Give more time for processing

        // Act - Simulate truncation
        File.WriteAllText(_testLogFile, ""); // Truncate
        await Task.Delay(200); // Give time for truncation to be detected
        await File.WriteAllLinesAsync(_testLogFile, newLines);
        await Task.Delay(1000); // Give more time for processing

        // Assert
        Assert.Equal(4, _processedLines.Count);
        Assert.Equal(initialLines.Concat(newLines), _processedLines);
    }

    [Fact]
    public async Task Should_Stop_On_Dispose()
    {
        // Arrange
        var lines = new[] { "Line 1", "Line 2" };
        var tailer = new LogTailer(_testLogFile, ProcessLine, 100);

        // Act
        await File.WriteAllLinesAsync(_testLogFile, lines);
        await Task.Delay(1000); // Give more time for processing
        tailer.Dispose();

        // Write more lines after dispose
        await File.WriteAllLinesAsync(_testLogFile, new[] { "Line 3" });
        await Task.Delay(1000); // Give more time to ensure no processing occurs

        // Assert
        Assert.Equal(2, _processedLines.Count); // Only first two lines processed
        Assert.Equal(lines, _processedLines);
    }

    [Fact]
    public async Task Should_Handle_IOException()
    {
        // Arrange
        var lines = new[] { "Line 1", "Line 2" };
        var tailer = new LogTailer(_testLogFile, ProcessLine, 100);

        try
        {
            // Act - Write initial lines
            await File.WriteAllLinesAsync(_testLogFile, lines);
            await Task.Delay(500); // Wait longer for initial processing to complete

            // Clear processed lines to ensure we only get new content
            lock (_processedLines)
            {
                _processedLines.Clear();
            }

            // Simulate IOException by renaming the file temporarily
            var tempPath = _testLogFile + ".temp";
            File.Move(_testLogFile, tempPath);
            await Task.Delay(500); // Wait longer for file system to stabilize

            // Create a new empty file and then write the new line
            File.WriteAllText(_testLogFile, ""); // Create empty file
            await Task.Delay(200); // Wait for file creation to be detected
            await File.AppendAllLinesAsync(_testLogFile, new[] { "Line 3" });
            await Task.Delay(500); // Wait longer for processing to complete

            // Assert
            var line = Assert.Single(_processedLines); // Should only see the new line
            Assert.Equal("Line 3", line);
        }
        finally
        {
            // Cleanup
            tailer.Dispose();
            if (File.Exists(_testLogFile + ".temp"))
            {
                File.Delete(_testLogFile + ".temp");
            }
        }
    }

    [Fact]
    public async Task Should_Handle_General_Exception()
    {
        // Arrange
        var mockProcessLine = new Mock<Action<string, string>>();
        mockProcessLine.Setup(x => x.Invoke(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new InvalidOperationException("Test exception"));

        var tailer = new LogTailer(_testLogFile, mockProcessLine.Object, 100);

        // Act
        await File.WriteAllLinesAsync(_testLogFile, new[] { "Line 1" });
        await Task.Delay(1000);

        // Assert
        mockProcessLine.Verify(x => x.Invoke(It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
    }

    private void ProcessLine(string filePath, string line)
    {
        lock (_processedLines)
        {
            _processedLines.Add(line);
        }
    }

    /// <summary>
    /// Cleans up test resources by deleting the temporary test file.
    /// </summary>
    public void Dispose()
    {
        try
        {
            if (File.Exists(_testLogFile))
            {
                File.Delete(_testLogFile);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
        GC.SuppressFinalize(this);
    }
} 