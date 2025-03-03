using Microsoft.Extensions.Configuration;
using FluentAssertions;

namespace CursorMCPMonitor.Tests;

public class ConfigurationTests
{
    [Fact]
    public void Should_Use_Default_Logs_Path_When_Not_Configured()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LogsRoot"] = null
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
        logsRoot.Should().Be(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Cursor",
            "logs"
        ));
    }

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
        logsRoot.Should().Be(customPath);
    }

    [Fact]
    public void Should_Use_Default_Poll_Interval_When_Not_Configured()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        var pollInterval = config.GetValue<int>("PollIntervalMs", 1000);

        // Assert
        pollInterval.Should().Be(1000);
    }

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
        pollInterval.Should().Be(interval);
    }
} 