using CursorMCPMonitor.Configuration;

namespace CursorMCPMonitor.Tests;

public class CommandLineOptionsTests
{
    [Fact]
    public void Should_Parse_LogsRoot_Option()
    {
        // Arrange
        var args = new[] { "--logs-root", "/path/to/logs" };

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        Assert.Equal("/path/to/logs", options["LogsRoot"]);
    }

    [Fact]
    public void Should_Parse_LogsRoot_Short_Option()
    {
        // Arrange
        var args = new[] { "-l", "/path/to/logs" };

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        Assert.Equal("/path/to/logs", options["LogsRoot"]);
    }

    [Fact]
    public void Should_Parse_PollInterval_Option()
    {
        // Arrange
        var args = new[] { "--poll-interval", "2000" };

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        Assert.Equal("2000", options["PollIntervalMs"]);
    }

    [Fact]
    public void Should_Parse_PollInterval_Short_Option()
    {
        // Arrange
        var args = new[] { "-p", "2000" };

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        Assert.Equal("2000", options["PollIntervalMs"]);
    }

    [Theory]
    [InlineData("debug")]
    [InlineData("info")]
    [InlineData("warning")]
    [InlineData("error")]
    public void Should_Parse_Verbosity_Option(string level)
    {
        // Arrange
        var args = new[] { "--verbosity", level };

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        Assert.Equal(level, options["Verbosity"]);
    }

    [Theory]
    [InlineData("debug")]
    [InlineData("info")]
    [InlineData("warning")]
    [InlineData("error")]
    public void Should_Parse_Verbosity_Short_Option(string level)
    {
        // Arrange
        var args = new[] { "-v", level };

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        Assert.Equal(level, options["Verbosity"]);
    }

    [Fact]
    public void Should_Parse_LogPattern_Option()
    {
        // Arrange
        var args = new[] { "--log-pattern", "*.log" };

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        Assert.Equal("*.log", options["LogPattern"]);
    }

    [Fact]
    public void Should_Parse_LogPattern_Short_Option()
    {
        // Arrange
        var args = new[] { "-f", "*.log" };

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        Assert.Equal("*.log", options["LogPattern"]);
    }

    [Fact]
    public void Should_Parse_Filter_Option()
    {
        // Arrange
        var args = new[] { "--filter", "error" };

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        Assert.Equal("error", options["Filter"]);
    }

    [Fact]
    public void Should_Parse_Multiple_Options()
    {
        // Arrange
        var args = new[] { 
            "--logs-root", "/path/to/logs",
            "--poll-interval", "2000",
            "--verbosity", "debug",
            "--log-pattern", "*.log",
            "--filter", "error"
        };

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        Assert.Equal("/path/to/logs", options["LogsRoot"]);
        Assert.Equal("2000", options["PollIntervalMs"]);
        Assert.Equal("debug", options["Verbosity"]);
        Assert.Equal("*.log", options["LogPattern"]);
        Assert.Equal("error", options["Filter"]);
    }

    [Fact]
    public void Should_Handle_No_Options()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        Assert.Empty(options);
    }
} 