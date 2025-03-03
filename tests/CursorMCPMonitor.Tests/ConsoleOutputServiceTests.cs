using CursorMCPMonitor.Services;
using Microsoft.Extensions.Logging;

namespace CursorMCPMonitor.Tests;

public class ConsoleOutputServiceTests : IDisposable
{
    private readonly Mock<ILogger<ConsoleOutputService>> _loggerMock;
    private readonly ConsoleOutputService _service;
    private readonly StringWriter _consoleOutput;
    private readonly TextWriter _originalOutput;

    public ConsoleOutputServiceTests()
    {
        _loggerMock = new Mock<ILogger<ConsoleOutputService>>();
        _service = new ConsoleOutputService(_loggerMock.Object);
        
        // Redirect console output for testing
        _originalOutput = Console.Out;
        _consoleOutput = new StringWriter();
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        // Restore original console output
        Console.SetOut(_originalOutput);
        _consoleOutput.Dispose();
    }

    [Fact]
    public void Should_Write_Raw_Message()
    {
        // Arrange
        var prefix = "Test:";
        var message = "Raw message";

        // Act
        _service.WriteRaw(prefix, message);
        var output = _consoleOutput.ToString();

        // Assert
        Assert.Contains($"{prefix} {message}", output);
        VerifyLoggerCalled("Raw", prefix, message, LogLevel.Information);
    }

    [Fact]
    public void Should_Write_Info_Message()
    {
        // Arrange
        var prefix = "Info:";
        var message = "Info message";

        // Act
        _service.WriteInfo(prefix, message);
        var output = _consoleOutput.ToString();

        // Assert
        Assert.Contains($"{prefix} {message}", output);
        VerifyLoggerCalled("Info", prefix, message, LogLevel.Information);
    }

    [Fact]
    public void Should_Write_Success_Message()
    {
        // Arrange
        var prefix = "Success:";
        var message = "Success message";

        // Act
        _service.WriteSuccess(prefix, message);
        var output = _consoleOutput.ToString();

        // Assert
        Assert.Contains($"{prefix} {message}", output);
        VerifyLoggerCalled("Success", prefix, message, LogLevel.Information);
    }

    [Fact]
    public void Should_Write_Warning_Message()
    {
        // Arrange
        var prefix = "Warning:";
        var message = "Warning message";

        // Act
        _service.WriteWarning(prefix, message);
        var output = _consoleOutput.ToString();

        // Assert
        Assert.Contains($"{prefix} {message}", output);
        VerifyLoggerCalled("Warning", prefix, message, LogLevel.Warning);
    }

    [Fact]
    public void Should_Write_Error_Message()
    {
        // Arrange
        var prefix = "Error:";
        var message = "Error message";

        // Act
        _service.WriteError(prefix, message);
        var output = _consoleOutput.ToString();

        // Assert
        Assert.Contains($"{prefix} {message}", output);
        VerifyLoggerCalled("Error", prefix, message, LogLevel.Error);
    }

    [Fact]
    public void Should_Write_Highlight_Message()
    {
        // Arrange
        var prefix = "Highlight:";
        var message = "Highlight message";

        // Act
        _service.WriteHighlight(prefix, message);
        var output = _consoleOutput.ToString();

        // Assert
        Assert.Contains($"{prefix} {message}", output);
        VerifyLoggerCalled("Highlight", prefix, message, LogLevel.Information);
    }

    [Fact]
    public async Task Should_Handle_Concurrent_Writes()
    {
        // Arrange
        var tasks = new List<Task>();
        var messages = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            var message = $"Message {i}";
            messages.Add(message);
            tasks.Add(Task.Run(() => _service.WriteRaw("Test:", message)));
        }

        // Act
        await Task.WhenAll(tasks);
        var output = _consoleOutput.ToString();

        // Assert
        foreach (var message in messages)
        {
            Assert.Contains(message, output);
        }
    }

    private void VerifyLoggerCalled(string outputType, string prefix, string message, LogLevel level)
    {
        _loggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => 
                    o.ToString()!.Contains(outputType) && 
                    o.ToString()!.Contains(prefix) && 
                    o.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
} 