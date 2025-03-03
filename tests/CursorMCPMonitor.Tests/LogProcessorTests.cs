using CursorMCPMonitor.Interfaces;
using CursorMCPMonitor.Services;
using Microsoft.Extensions.Logging;

namespace CursorMCPMonitor.Tests;

public class LogProcessorTests
{
    private readonly Mock<ILogger<LogProcessorService>> _loggerMock;
    private readonly Mock<IConsoleOutputService> _consoleOutputMock;
    private readonly LogProcessorService _processor;

    public LogProcessorTests()
    {
        _loggerMock = new Mock<ILogger<LogProcessorService>>();
        _consoleOutputMock = new Mock<IConsoleOutputService>();
        _processor = new LogProcessorService(_consoleOutputMock.Object, _loggerMock.Object);
    }

    [Theory]
    [InlineData("unrecognized_keys in config", "UnrecognizedKeys")]
    [InlineData("No server info found", "NoServerInfo")]
    [InlineData("No workspace folders found", "Warning")]
    [InlineData("random unstructured line", "Raw")]
    public void Should_Process_Unstructured_Lines_Correctly(string input, string expectedType)
    {
        // Arrange
        var filePath = "test.log";

        // Act
        _processor.ProcessLogLine(filePath, input);

        // Assert
        switch (expectedType)
        {
            case "UnrecognizedKeys":
                _consoleOutputMock.Verify(x => x.WriteError("[UnrecognizedKeys]", input), Times.Once);
                break;
            case "NoServerInfo":
                _consoleOutputMock.Verify(x => x.WriteError("[NoServerInfo]", input), Times.Once);
                break;
            case "Warning":
                _consoleOutputMock.Verify(x => x.WriteWarning("[Warning]", input), Times.Once);
                break;
            case "Raw":
                _consoleOutputMock.Verify(x => x.WriteRaw("[Raw]", input), Times.Once);
                break;
        }
    }

    [Theory]
    [InlineData("debug", LogLevel.Debug)]
    [InlineData("information", LogLevel.Information)]
    [InlineData("info", LogLevel.Information)]
    [InlineData("warning", LogLevel.Warning)]
    [InlineData("warn", LogLevel.Warning)]
    [InlineData("error", LogLevel.Error)]
    [InlineData("err", LogLevel.Error)]
    [InlineData("invalid", LogLevel.Information)]
    public void Should_Set_Correct_Verbosity_Level(string level, LogLevel expected)
    {
        // Act
        _processor.SetVerbosityLevel(level);

        // Verify log was written
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expected.ToString())),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("2024-03-02 12:26:34.698 [error] a602: Error message", LogLevel.Error, true)]
    [InlineData("2024-03-02 12:26:34.698 [warning] a602: Warning message", LogLevel.Warning, true)]
    [InlineData("2024-03-02 12:26:34.698 [info] a602: Info message", LogLevel.Information, false)]
    [InlineData("2024-03-02 12:26:34.698 [debug] a602: Debug message", LogLevel.Debug, false)]
    public void Should_Filter_By_Verbosity_Level(string input, LogLevel messageLevel, bool shouldShow)
    {
        // Arrange
        _processor.SetVerbosityLevel("warning"); // Set to warning level
        var filePath = "test.log";

        // Act
        _processor.ProcessLogLine(filePath, input);

        // Assert
        if (shouldShow)
        {
            if (messageLevel == LogLevel.Error)
            {
                _consoleOutputMock.Verify(x => x.WriteError(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            }
            else // Warning
            {
                _consoleOutputMock.Verify(x => x.WriteWarning(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            }
        }
        else
        {
            _consoleOutputMock.Verify(x => x.WriteError(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _consoleOutputMock.Verify(x => x.WriteWarning(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
} 