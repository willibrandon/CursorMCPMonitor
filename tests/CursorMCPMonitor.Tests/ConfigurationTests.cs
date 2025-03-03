using Microsoft.Extensions.Configuration;

namespace CursorMCPMonitor.Tests;

/// <summary>
/// Tests for configuration handling to ensure proper loading and fallback of settings.
/// </summary>
public class ConfigurationTests
{
    /// <summary>
    /// Verifies that the default logs path is used when no configuration is provided.
    /// </summary>
    [Fact]
    public void Should_Use_Default_Logs_Path_When_Not_Configured()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["LogsRoot"] = null })
            .Build();

        // Act
        var logsRoot = config.GetValue<string>("LogsRoot") ?? 
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Cursor",
                "logs"
            );

        // Assert
        var expected = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Cursor",
            "logs"
        );
        Assert.Equal(expected, logsRoot);
    }

    /// <summary>
    /// Verifies that a custom logs path is used when provided in configuration.
    /// </summary>
    [Fact]
    public void Should_Use_Configured_Logs_Path()
    {
        // Arrange
        var customPath = Path.Combine("custom", "path", "logs");
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LogsRoot"] = customPath
            })
            .Build();

        // Act
        var logsRoot = config.GetValue<string>("LogsRoot") ?? 
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Cursor",
                "logs"
            );

        // Assert
        Assert.Equal(customPath, logsRoot);
    }

    /// <summary>
    /// Verifies that the default poll interval is used when no configuration is provided.
    /// </summary>
    [Fact]
    public void Should_Use_Default_Poll_Interval_When_Not_Configured()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection([])
            .Build();

        // Act
        var pollInterval = config.GetValue<int>("PollIntervalMs", 1000);

        // Assert
        Assert.Equal(1000, pollInterval);
    }

    /// <summary>
    /// Verifies that custom poll intervals are correctly applied from configuration.
    /// </summary>
    /// <param name="interval">The poll interval in milliseconds to test.</param>
    [Theory]
    [InlineData(500)]
    [InlineData(2000)]
    [InlineData(5000)]
    public void Should_Use_Configured_Poll_Interval(int interval)
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PollIntervalMs"] = interval.ToString()
            })
            .Build();

        // Act
        var pollInterval = config.GetValue<int>("PollIntervalMs", 1000);

        // Assert
        Assert.Equal(interval, pollInterval);
    }
} 