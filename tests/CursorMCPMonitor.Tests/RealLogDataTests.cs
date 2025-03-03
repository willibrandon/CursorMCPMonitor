namespace CursorMCPMonitor.Tests;

/// <summary>
/// Integration tests using real log data to verify the log monitoring system's behavior.
/// </summary>
public class RealLogDataTests : IDisposable
{
    private readonly string _testFilePath;
    private readonly List<(string FilePath, string Line)> _receivedLines = [];
    private readonly string _sourceLogFile;

    /// <summary>
    /// Initializes a new instance of the RealLogDataTests class.
    /// Sets up test paths and initializes the test environment.
    /// </summary>
    public RealLogDataTests()
    {
        _testFilePath = Path.Combine(Path.GetTempPath(), $"test_log_{Guid.NewGuid()}.log");
        _sourceLogFile = Path.Combine(AppContext.BaseDirectory, "data", "all_mcp_logs.txt");
    }

    [Fact]
    public async Task Should_Process_Real_Log_Data()
    {
        // Arrange
        File.WriteAllText(_testFilePath, string.Empty);
        using var tailer = new LogTailer(_testFilePath, OnLineReceived);
        await Task.Delay(100); // Give the tailer time to initialize

        // Act - Copy first 10 lines from real log file
        var firstTenLines = File.ReadLines(_sourceLogFile).Take(10).ToList();
        await File.WriteAllLinesAsync(_testFilePath, firstTenLines);
        await Task.Delay(2000); // Give the tailer time to process

        // Assert
        _receivedLines.Should().HaveCount(10);
        for (int i = 0; i < 10; i++)
        {
            _receivedLines[i].Line.Should().Be(firstTenLines[i]);
        }
    }

    [Fact]
    public async Task Should_Handle_Log_Rotation_With_Real_Data()
    {
        // Arrange
        File.WriteAllText(_testFilePath, string.Empty);
        using var tailer = new LogTailer(_testFilePath, OnLineReceived);
        await Task.Delay(100);

        // Act 1 - Write first batch
        var firstBatch = File.ReadLines(_sourceLogFile).Take(5).ToList();
        await File.WriteAllLinesAsync(_testFilePath, firstBatch);
        await Task.Delay(2000);

        // Act 2 - Simulate rotation with new content
        File.Delete(_testFilePath);
        var secondBatch = File.ReadLines(_sourceLogFile).Skip(5).Take(5).ToList();
        await File.WriteAllLinesAsync(_testFilePath, secondBatch);
        await Task.Delay(2000);

        // Assert
        _receivedLines.Should().HaveCount(10);
        for (int i = 0; i < 5; i++)
        {
            _receivedLines[i].Line.Should().Be(firstBatch[i]);
            _receivedLines[i + 5].Line.Should().Be(secondBatch[i]);
        }
    }

    [Fact]
    public async Task Should_Handle_Incremental_Updates()
    {
        // Arrange
        File.WriteAllText(_testFilePath, string.Empty);
        using var tailer = new LogTailer(_testFilePath, OnLineReceived);
        await Task.Delay(100);

        // Act - Write lines one by one
        var lines = File.ReadLines(_sourceLogFile).Take(5).ToList();
        foreach (var line in lines)
        {
            await File.AppendAllTextAsync(_testFilePath, line + Environment.NewLine);
            await Task.Delay(500); // Give time between each line
        }
        await Task.Delay(1000); // Final wait

        // Assert
        _receivedLines.Should().HaveCount(5);
        for (int i = 0; i < 5; i++)
        {
            _receivedLines[i].Line.Should().Be(lines[i]);
        }
    }

    [Fact]
    public async Task Should_Parse_Valid_Log_Lines()
    {
        // Arrange
        var validLines = File.ReadLines(_sourceLogFile)
            .Where(line => line.Contains("[info]") || line.Contains("[error]") || line.Contains("[warning]"))
            .Take(10)
            .ToList();

        File.WriteAllText(_testFilePath, string.Empty);
        using var tailer = new LogTailer(_testFilePath, OnLineReceived);
        await Task.Delay(100);

        // Act
        await File.WriteAllLinesAsync(_testFilePath, validLines);
        await Task.Delay(2000);

        // Assert
        _receivedLines.Should().HaveCount(10);
        foreach (var (_, line) in _receivedLines)
        {
            // Verify each line matches our log pattern
            line.Should().MatchRegex(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} \[(info|error|warning)\].*$");
        }
    }

    private void OnLineReceived(string filePath, string line)
    {
        _receivedLines.Add((filePath, line));
    }

    /// <summary>
    /// Cleans up test resources by deleting the temporary test file.
    /// </summary>
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