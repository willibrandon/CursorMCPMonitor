using CursorMCPMonitor.Configuration;
using CursorMCPMonitor.Interfaces;
using CursorMCPMonitor.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace CursorMCPMonitor.Tests;

public class LogProcessorTests
{
    private readonly Mock<ILogger<LogProcessorService>> _loggerMock;
    private readonly Mock<IConsoleOutputService> _consoleOutputMock;
    private readonly Mock<IWebSocketService> _webSocketServiceMock;
    private readonly LogProcessorService _processor;
    private readonly AppConfig _config;
    private readonly string _testFilePath = "test.log";

    public LogProcessorTests()
    {
        _loggerMock = new Mock<ILogger<LogProcessorService>>();
        _consoleOutputMock = new Mock<IConsoleOutputService>();
        _webSocketServiceMock = new Mock<IWebSocketService>();
        _config = new AppConfig
        {
            Verbosity = "info",
            Filter = null
        };
        _processor = new LogProcessorService(_consoleOutputMock.Object, _loggerMock.Object, _webSocketServiceMock.Object);
    }

    [Theory]
    [InlineData("2024-03-02 12:26:34.698 [info] a602: Test message", true)]
    [InlineData("2024-03-02 12:26:34.698 [debug] a602: Debug message", false)]
    [InlineData("2024-03-02 12:26:34.698 [warning] a602: Warning message", true)]
    [InlineData("2024-03-02 12:26:34.698 [error] a602: Error message", true)]
    public void Should_Filter_By_Verbosity_Level(string logLine, bool shouldProcess)
    {
        // Arrange
        _processor.SetVerbosityLevel("info");

        // Act
        _processor.ProcessLogLine(_testFilePath, logLine);

        // Assert
        if (shouldProcess)
        {
            if (logLine.Contains("[error]"))
            {
                _consoleOutputMock.Verify(x => x.WriteError(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            }
            else if (logLine.Contains("[warning]"))
            {
                _consoleOutputMock.Verify(x => x.WriteWarning(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            }
            else
            {
                _consoleOutputMock.Verify(x => x.WriteInfo(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            }
        }
        else
        {
            _consoleOutputMock.Verify(x => x.WriteInfo(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _consoleOutputMock.Verify(x => x.WriteWarning(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _consoleOutputMock.Verify(x => x.WriteError(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }

    [Theory]
    [InlineData("debug", "2024-03-02 12:26:34.698 [debug] a602: Debug message", true)]
    [InlineData("info", "2024-03-02 12:26:34.698 [debug] a602: Debug message", false)]
    [InlineData("warning", "2024-03-02 12:26:34.698 [info] a602: Info message", false)]
    [InlineData("error", "2024-03-02 12:26:34.698 [warning] a602: Warning message", false)]
    public void Should_Respect_Different_Verbosity_Settings(string verbosity, string logLine, bool shouldProcess)
    {
        // Arrange
        _processor.SetVerbosityLevel(verbosity);

        // Act
        _processor.ProcessLogLine(_testFilePath, logLine);

        // Assert
        if (shouldProcess)
        {
            if (logLine.Contains("[debug]"))
            {
                _consoleOutputMock.Verify(x => x.WriteInfo(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            }
        }
        else
        {
            _consoleOutputMock.Verify(x => x.WriteInfo(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _consoleOutputMock.Verify(x => x.WriteWarning(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _consoleOutputMock.Verify(x => x.WriteError(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }

    [Theory]
    [InlineData("error", "2024-03-02 12:26:34.698 [error] a602: Test error message")]
    [InlineData("test", "2024-03-02 12:26:34.698 [info] a602: Test message")]
    [InlineData("warning", "2024-03-02 12:26:34.698 [warning] a602: Warning test")]
    public void Should_Apply_Filter_Pattern(string filter, string logLine)
    {
        // Arrange
        _processor.SetVerbosityLevel("debug"); // Allow all levels
        _processor.SetFilter(filter);

        // Act
        _processor.ProcessLogLine(_testFilePath, logLine);

        // Assert
        if (logLine.Contains("[error]"))
        {
            _consoleOutputMock.Verify(x => x.WriteError(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
        else if (logLine.Contains("[warning]"))
        {
            _consoleOutputMock.Verify(x => x.WriteWarning(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
        else
        {
            _consoleOutputMock.Verify(x => x.WriteInfo(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
    }

    [Theory]
    [InlineData("error", "2024-03-02 12:26:34.698 [info] a602: Success message")]
    [InlineData("test", "2024-03-02 12:26:34.698 [info] a602: Different message")]
    public void Should_Not_Process_Non_Matching_Filter(string filter, string logLine)
    {
        // Arrange
        _processor.SetFilter(filter);

        // Act
        _processor.ProcessLogLine(_testFilePath, logLine);

        // Assert
        _consoleOutputMock.Verify(x => x.WriteInfo(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _consoleOutputMock.Verify(x => x.WriteWarning(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _consoleOutputMock.Verify(x => x.WriteError(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Invalid log line")]
    [InlineData("2024-03-02 12:26:34.698 Missing level bracket")]
    [InlineData("2024-03-02 12:26:34.698 [info] Missing client id")]
    [InlineData("[info] a602: Missing timestamp")]
    public void Should_Handle_Invalid_Log_Lines(string logLine)
    {
        // Act
        _processor.ProcessLogLine(_testFilePath, logLine);

        // Assert
        if (string.IsNullOrWhiteSpace(logLine))
        {
            _consoleOutputMock.Verify(x => x.WriteRaw(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
        else
        {
            _consoleOutputMock.Verify(x => x.WriteRaw("[Raw]", logLine), Times.Once);
        }
    }

    [Theory]
    [InlineData("2024-03-02 12:26:34.698 [info] a602: Test message", "info")]
    [InlineData("2024-03-02 12:26:34.698 [debug] a602: Debug message", "debug")]
    [InlineData("2024-03-02 12:26:34.698 [warning] a602: Warning message", "warning")]
    [InlineData("2024-03-02 12:26:34.698 [error] a602: Error message", "error")]
    public void Should_Process_Different_Message_Types(string logLine, string expectedLevel)
    {
        // Arrange
        _processor.SetVerbosityLevel("debug"); // Allow all levels

        // Act
        _processor.ProcessLogLine(_testFilePath, logLine);

        // Assert
        switch (expectedLevel)
        {
            case "error":
                _consoleOutputMock.Verify(x => x.WriteError(It.IsAny<string>(), It.Is<string>(s => s.Contains("Error message"))), Times.Once);
                break;
            case "warning":
                _consoleOutputMock.Verify(x => x.WriteWarning(It.IsAny<string>(), It.Is<string>(s => s.Contains("Warning message"))), Times.Once);
                break;
            case "info":
                _consoleOutputMock.Verify(x => x.WriteInfo(It.IsAny<string>(), It.Is<string>(s => s.Contains("Test message"))), Times.Once);
                break;
            case "debug":
                _consoleOutputMock.Verify(x => x.WriteInfo(It.IsAny<string>(), It.Is<string>(s => s.Contains("Debug message"))), Times.Once);
                break;
        }
    }

    [Theory]
    [InlineData("2024-03-02 12:26:34.698 [unknown] a602: Test message")]
    [InlineData("2024-03-02 12:26:34.698 [] a602: Test message")]
    public void Should_Handle_Unknown_Log_Level(string logLine)
    {
        // Arrange
        _processor.SetVerbosityLevel("debug"); // Allow all levels

        // Act
        _processor.ProcessLogLine(_testFilePath, logLine);

        // Assert
        _consoleOutputMock.Verify(x => x.WriteRaw(It.IsAny<string>(), logLine), Times.Once);
    }

    [Theory]
    [InlineData("2024-03-02 12:26:34.698 [info] a602: Test message", "info")]
    [InlineData("2024-03-02 12:26:34.698 [debug] a602: Debug message", "debug")]
    [InlineData("2024-03-02 12:26:34.698 [warning] a602: Warning message", "warning")]
    [InlineData("2024-03-02 12:26:34.698 [error] a602: Error message", "error")]
    public void Should_Parse_Log_Level_Correctly(string logLine, string expectedLevel)
    {
        // Arrange
        _processor.SetVerbosityLevel("debug"); // Allow all levels

        // Act
        _processor.ProcessLogLine(_testFilePath, logLine);

        // Assert
        if (expectedLevel == "error")
        {
            _consoleOutputMock.Verify(x => x.WriteError(It.IsAny<string>(), It.Is<string>(s => s.Contains($"[{expectedLevel}]"))), Times.Once);
        }
        else if (expectedLevel == "warning")
        {
            _consoleOutputMock.Verify(x => x.WriteWarning(It.IsAny<string>(), It.Is<string>(s => s.Contains($"[{expectedLevel}]"))), Times.Once);
        }
        else
        {
            _consoleOutputMock.Verify(x => x.WriteInfo(It.IsAny<string>(), It.Is<string>(s => s.Contains($"[{expectedLevel}]"))), Times.Once);
        }
    }

    [Theory]
    [InlineData("2024-03-02 12:26:34.698 [info] a602: Test message", "a602")]
    [InlineData("2024-03-02 12:26:34.698 [debug] xyz9: Debug message", "xyz9")]
    public void Should_Parse_Client_Id_Correctly(string logLine, string expectedClientId)
    {
        // Arrange
        _processor.SetVerbosityLevel("debug"); // Allow all levels

        // Act
        _processor.ProcessLogLine(_testFilePath, logLine);

        // Assert
        _consoleOutputMock.Verify(
            x => x.WriteInfo(
                It.IsAny<string>(),
                It.Is<string>(s => s.Contains($"[Client: {expectedClientId}]"))),
            Times.Once);
    }
} 