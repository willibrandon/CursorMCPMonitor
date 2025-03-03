using System.Text.RegularExpressions;

namespace CursorMCPMonitor.Tests;

/// <summary>
/// Tests for log line parsing functionality to ensure correct pattern matching and data extraction.
/// </summary>
public partial class LogParsingTests
{
    [GeneratedRegex(@"^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}) \[(?<level>\w+)\]\s+(?<clientId>\w+):\s+(?<message>.*)$", RegexOptions.Compiled | RegexOptions.ExplicitCapture)]
    private static partial Regex LogLineRegex();

    /// <summary>
    /// Tests that valid log lines are correctly parsed with the expected timestamp, level, client ID, and message.
    /// </summary>
    /// <param name="input">The log line to parse.</param>
    /// <param name="expectedTimestamp">Expected timestamp from the log line.</param>
    /// <param name="expectedLevel">Expected log level (info, error, warning).</param>
    /// <param name="expectedClientId">Expected client identifier.</param>
    /// <param name="expectedMessage">Expected message content.</param>
    [Theory]
    [InlineData(
        "2024-03-02 12:26:34.698 [info] a602: Handling CreateClient action",
        "2024-03-02 12:26:34.698", "info", "a602", "Handling CreateClient action")]
    [InlineData(
        "2024-03-02 15:45:12.123 [error] b123: Error in MCP: Connection failed",
        "2024-03-02 15:45:12.123", "error", "b123", "Error in MCP: Connection failed")]
    [InlineData(
        "2024-03-02 18:30:00.001 [warning] xyz9: No workspace folders found",
        "2024-03-02 18:30:00.001", "warning", "xyz9", "No workspace folders found")]
    public void Should_Parse_Valid_Log_Lines(string input, string expectedTimestamp, 
        string expectedLevel, string expectedClientId, string expectedMessage)
    {
        // Act
        var match = LogLineRegex().Match(input);

        // Assert
        Assert.True(match.Success);
        Assert.Equal(expectedTimestamp, match.Groups["timestamp"].Value);
        Assert.Equal(expectedLevel, match.Groups["level"].Value);
        Assert.Equal(expectedClientId, match.Groups["clientId"].Value);
        Assert.Equal(expectedMessage, match.Groups["message"].Value);
    }

    [Theory]
    [InlineData("Invalid log line format")]
    [InlineData("2024-03-02 12:26:34.698 [info] Missing client and message")]
    [InlineData("[info] a602: Missing timestamp")]
    [InlineData("2024-03-02 12:26:34.698 a602: Missing level brackets")]
    public void Should_Not_Parse_Invalid_Log_Lines(string input)
    {
        // Act
        var match = LogLineRegex().Match(input);

        // Assert
        Assert.False(match.Success);
    }
} 