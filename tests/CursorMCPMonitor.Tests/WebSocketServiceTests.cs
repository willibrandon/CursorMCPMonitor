using CursorMCPMonitor.Services;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;

namespace CursorMCPMonitor.Tests;

public class WebSocketServiceTests : IDisposable
{
    private readonly Mock<ILogger<WebSocketService>> _loggerMock;
    private readonly WebSocketService _service;
    private readonly Mock<WebSocket> _webSocketMock;

    public WebSocketServiceTests()
    {
        _loggerMock = new Mock<ILogger<WebSocketService>>();
        _service = new WebSocketService(_loggerMock.Object);
        _webSocketMock = new Mock<WebSocket>();
    }

    public void Dispose()
    {
        _service.Dispose();
    }

    [Fact]
    public async Task HandleClientAsync_NewConnection_AddsClient()
    {
        // Arrange
        var messageReceived = new TaskCompletionSource<bool>();
        _webSocketMock.Setup(ws => ws.State).Returns(WebSocketState.Open);
        _webSocketMock
            .Setup(ws => ws.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await messageReceived.Task;
                return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);
            });

        // Act
        var clientTask = _service.HandleClientAsync(_webSocketMock.Object);
        await Task.Delay(100); // Brief delay to ensure client is added

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("New WebSocket client connected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Cleanup
        messageReceived.SetResult(true);
        await clientTask;
    }

    [Fact]
    public async Task BroadcastAsync_WithActiveClient_SendsMessage()
    {
        // Arrange
        var messageReceived = new TaskCompletionSource<bool>();
        _webSocketMock.Setup(ws => ws.State).Returns(WebSocketState.Open);
        _webSocketMock
            .Setup(ws => ws.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await messageReceived.Task;
                return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);
            });

        _webSocketMock
            .Setup(ws => ws.SendAsync(It.IsAny<ArraySegment<byte>>(), WebSocketMessageType.Text, true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clientTask = _service.HandleClientAsync(_webSocketMock.Object);
        await Task.Delay(100); // Brief delay to ensure client is added

        var message = new { text = "Test message" };

        // Act
        await _service.BroadcastAsync(message);

        // Assert
        _webSocketMock.Verify(
            ws => ws.SendAsync(
                It.IsAny<ArraySegment<byte>>(),
                WebSocketMessageType.Text,
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Cleanup
        messageReceived.SetResult(true);
        await clientTask;
    }

    [Fact]
    public async Task BroadcastAsync_WithClosedClient_RemovesClient()
    {
        // Arrange
        var messageReceived = new TaskCompletionSource<bool>();
        _webSocketMock.Setup(ws => ws.State).Returns(WebSocketState.Open);
        _webSocketMock
            .Setup(ws => ws.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await messageReceived.Task;
                return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);
            });

        var clientTask = _service.HandleClientAsync(_webSocketMock.Object);
        await Task.Delay(100); // Brief delay to ensure client is added

        // Change client state to closed
        _webSocketMock.Setup(ws => ws.State).Returns(WebSocketState.Closed);

        var message = new { text = "Test message" };

        // Act
        await _service.BroadcastAsync(message);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Removed dead WebSocket client")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Cleanup
        messageReceived.SetResult(true);
        await clientTask;
    }
} 