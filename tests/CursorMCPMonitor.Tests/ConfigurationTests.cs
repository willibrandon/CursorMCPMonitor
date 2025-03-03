using CursorMCPMonitor.Configuration;
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
            .AddInMemoryCollection()
            .Build();

        // Act
        var appConfig = AppConfig.Load(config);

        // Assert
        Assert.Equal(AppConfig.GetDefaultLogsDirectory(), appConfig.LogsRoot);
    }

    /// <summary>
    /// Verifies that a custom logs path is used when provided in configuration.
    /// </summary>
    [Fact]
    public void Should_Use_Configured_Logs_Path()
    {
        // Arrange
        var customPath = Path.Combine(Path.GetTempPath(), "CustomLogs");
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("LogsRoot", customPath)
            })
            .Build();

        // Act
        var appConfig = AppConfig.Load(config);

        // Assert
        Assert.Equal(customPath, appConfig.LogsRoot);
    }

    /// <summary>
    /// Verifies that the default poll interval is used when no configuration is provided.
    /// </summary>
    [Fact]
    public void Should_Use_Default_Poll_Interval_When_Not_Configured()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection()
            .Build();

        // Act
        var appConfig = AppConfig.Load(config);

        // Assert
        Assert.Equal(1000, appConfig.PollIntervalMs); // Default value
    }

    /// <summary>
    /// Verifies that custom poll intervals are correctly applied from configuration.
    /// </summary>
    /// <param name="interval">The poll interval in milliseconds to test.</param>
    [Theory]
    [InlineData(500)]
    [InlineData(1000)]
    [InlineData(2000)]
    public void Should_Use_Configured_Poll_Interval(int interval)
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("PollIntervalMs", interval.ToString())
            })
            .Build();

        // Act
        var appConfig = AppConfig.Load(config);

        // Assert
        Assert.Equal(interval, appConfig.PollIntervalMs);
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Information")]
    [InlineData("Warning")]
    [InlineData("Error")]
    public void Should_Use_Configured_Verbosity(string verbosity)
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Verbosity", verbosity)
            })
            .Build();

        // Act
        var appConfig = AppConfig.Load(config);

        // Assert
        Assert.Equal(verbosity, appConfig.Verbosity);
    }

    [Theory]
    [InlineData("error")]
    [InlineData("warning")]
    [InlineData("test")]
    public void Should_Use_Configured_Filter(string filter)
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Filter", filter)
            })
            .Build();

        // Act
        var appConfig = AppConfig.Load(config);

        // Assert
        Assert.Equal(filter, appConfig.Filter);
    }

    [Theory]
    [InlineData("Cursor MCP*.log")]
    [InlineData("*.log")]
    public void Should_Use_Configured_Log_Pattern(string pattern)
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("LogPattern", pattern)
            })
            .Build();

        // Act
        var appConfig = AppConfig.Load(config);

        // Assert
        Assert.Equal(pattern, appConfig.LogPattern);
    }
} 