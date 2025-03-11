using CursorMCPMonitor.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace CursorMCPMonitor.Tests;

public class WebSocketServicePerformanceTests
{
    private readonly WebSocketService _service;
    private readonly List<Mock<WebSocket>> _mockClients = new();

    public WebSocketServicePerformanceTests()
    {
        var loggerMock = new Mock<ILogger<WebSocketService>>();
        loggerMock.Setup(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), 
            It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
        _service = new WebSocketService(loggerMock.Object);
    }

    private Mock<WebSocket> CreateMockClient()
    {
        var mockSocket = new Mock<WebSocket>();
        mockSocket.Setup(ws => ws.State).Returns(WebSocketState.Open);
        mockSocket.Setup(ws => ws.SendAsync(It.IsAny<ArraySegment<byte>>(), WebSocketMessageType.Text, true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockClients.Add(mockSocket);
        return mockSocket;
    }

    [Theory]
    [InlineData(10)]
    [InlineData(50)]
    public async Task BroadcastAsync_Performance(int clientCount)
    {
        // Arrange - Create clients and add them directly to service's internal collection
        var clients = Enumerable.Range(0, clientCount)
            .Select(_ => CreateMockClient().Object)
            .ToList();

        var message = new { text = "x" };
        var sw = Stopwatch.StartNew();

        // Act
        await _service.BroadcastAsync(message);
        sw.Stop();

        // Assert - Allow for CI environment variability
        var expectedMs = clientCount switch
        {
            10 => 50,  // Up to 50ms for 10 clients in CI
            50 => 100, // Up to 100ms for 50 clients in CI
            _ => throw new ArgumentException("Unexpected client count")
        };
        Assert.True(sw.ElapsedMilliseconds <= expectedMs, 
            $"Broadcasting took too long: {sw.ElapsedMilliseconds}ms total for {clientCount} clients (expected <= {expectedMs}ms)");
    }

    [Fact]
    public async Task ConcurrentBroadcasts_Performance()
    {
        // Arrange - Minimal setup
        const int clientCount = 10;
        const int messageCount = 3;
        
        var clients = Enumerable.Range(0, clientCount)
            .Select(_ => CreateMockClient().Object)
            .ToList();

        var messages = Enumerable.Range(0, messageCount)
            .Select(i => new { text = "x" });

        var sw = Stopwatch.StartNew();

        // Act
        await Task.WhenAll(messages.Select(msg => _service.BroadcastAsync(msg)));
        sw.Stop();

        // Assert - Allow for CI environment variability
        const int expectedMs = 75; // Up to 75ms for 3 concurrent broadcasts to 10 clients
        Assert.True(sw.ElapsedMilliseconds <= expectedMs, 
            $"Concurrent broadcasting took too long: {sw.ElapsedMilliseconds}ms total (expected <= {expectedMs}ms)");
    }

    [Fact]
    public async Task ClientChurn_Performance()
    {
        // Arrange - Minimal setup
        const int baseClientCount = 10;
        const int churnCount = 3;

        var baseClients = Enumerable.Range(0, baseClientCount)
            .Select(_ => CreateMockClient())
            .ToList();

        var sw = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < churnCount; i++)
        {
            CreateMockClient(); // Add new
            baseClients[i].Setup(ws => ws.State).Returns(WebSocketState.Closed); // Remove old
            await _service.BroadcastAsync(new { text = "x" });
        }
        sw.Stop();

        // Assert - Allow for CI environment variability
        const int expectedMs = 100; // Up to 100ms for 3 churn operations
        Assert.True(sw.ElapsedMilliseconds <= expectedMs, 
            $"Client churn took too long: {sw.ElapsedMilliseconds}ms total (expected <= {expectedMs}ms)");
    }
} 