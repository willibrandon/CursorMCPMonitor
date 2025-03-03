namespace CursorMCPMonitor.Tests;

public class LogTailerTests : IDisposable
{
    private readonly string _testFilePath;
    private readonly List<(string FilePath, string Line)> _receivedLines = [];
    private static readonly string[] _testLines = ["Line 1", "Line 2"];

    public LogTailerTests()
    {
        _testFilePath = Path.Combine(Path.GetTempPath(), $"test_log_{Guid.NewGuid()}.log");
    }

    [Fact]
    public async Task Should_Detect_New_Lines()
    {
        // Arrange
        File.WriteAllText(_testFilePath, string.Empty); // Start with empty file
        using var tailer = new LogTailer(_testFilePath, OnLineReceived);
        await Task.Delay(100); // Give the tailer time to initialize

        // Act
        await File.AppendAllLinesAsync(_testFilePath, _testLines);
        await Task.Delay(2000); // Give the tailer time to process

        // Assert
        _receivedLines.Should().HaveCount(2);
        _receivedLines[0].Line.Should().Be(_testLines[0]);
        _receivedLines[1].Line.Should().Be(_testLines[1]);
    }

    [Fact]
    public async Task Should_Handle_File_Rotation()
    {
        // Arrange
        File.WriteAllText(_testFilePath, string.Empty); // Start with empty file
        using var tailer = new LogTailer(_testFilePath, OnLineReceived);
        await Task.Delay(100); // Give the tailer time to initialize

        // Act - Simulate log rotation
        File.Delete(_testFilePath);
        await File.WriteAllTextAsync(_testFilePath, "New log file\n");
        await Task.Delay(2000); // Give the tailer time to process

        // Assert
        _receivedLines.Should().ContainSingle()
            .Which.Line.Should().Be("New log file");
    }

    [Fact]
    public async Task Should_Handle_File_Truncation()
    {
        // Arrange
        File.WriteAllText(_testFilePath, string.Empty); // Start with empty file
        using var tailer = new LogTailer(_testFilePath, OnLineReceived);
        await Task.Delay(100); // Give the tailer time to initialize

        // Act - Write initial content then truncate
        await File.WriteAllTextAsync(_testFilePath, "New content after truncate\n");
        await Task.Delay(2000); // Give the tailer time to process

        // Assert
        _receivedLines.Should().ContainSingle()
            .Which.Line.Should().Be("New content after truncate");
    }

    [Fact]
    public void Should_Stop_On_Dispose()
    {
        // Arrange
        File.WriteAllText(_testFilePath, string.Empty); // Start with empty file
        var tailer = new LogTailer(_testFilePath, OnLineReceived);

        // Act
        tailer.Dispose();

        // Assert - No exception should be thrown
        tailer.Dispose(); // Second dispose should be safe
    }

    private void OnLineReceived(string filePath, string line)
    {
        _receivedLines.Add((filePath, line));
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_testFilePath))
            {
                File.Delete(_testFilePath);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }

        GC.SuppressFinalize(this);
    }
} 