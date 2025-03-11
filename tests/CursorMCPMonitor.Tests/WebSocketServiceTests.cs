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
    public async Task HandleClientAsync_NewConnection_AddsClientAndMaintainsConnection()
    {
        // Arrange
        _webSocketMock.Setup(ws => ws.State).Returns(WebSocketState.Open);
        var receiveResult = new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);
        _webSocketMock
            .Setup(ws => ws.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiveResult);

        // Act
        await _service.HandleClientAsync(_webSocketMock.Object);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("New WebSocket client connected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleClientAsync_ClientDisconnects_RemovesClientAndLogsDisconnection()
    {
        // Arrange
        _webSocketMock.Setup(ws => ws.State).Returns(WebSocketState.Open);
        var receiveResult = new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);
        _webSocketMock
            .Setup(ws => ws.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiveResult);

        // Act
        await _service.HandleClientAsync(_webSocketMock.Object);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("WebSocket client disconnected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task BroadcastAsync_WithConnectedClients_SendsMessageToAllClients()
    {
        // Arrange
        _webSocketMock.Setup(ws => ws.State).Returns(WebSocketState.Open);
        _webSocketMock
            .Setup(ws => ws.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebSocketReceiveResult(0, WebSocketMessageType.Text, true));
        _webSocketMock
            .Setup(ws => ws.SendAsync(It.IsAny<ArraySegment<byte>>(), WebSocketMessageType.Text, true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var message = new { text = "Test message" };

        // Add a client by handling a connection
        var receiveTask = Task.Run(async () =>
        {
            await _service.HandleClientAsync(_webSocketMock.Object);
        });

        // Wait a bit for the client to be added
        await Task.Delay(100);

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
    }

    [Fact]
    public async Task BroadcastAsync_WithDeadClient_RemovesDeadClient()
    {
        // Arrange
        var messageReceived = new TaskCompletionSource<bool>();
        _webSocketMock.Setup(ws => ws.State).Returns(WebSocketState.Open);
        _webSocketMock
            .Setup(ws => ws.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await messageReceived.Task; // Keep the connection alive until we're ready
                return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);
            });

        var message = new { text = "Test message" };

        // Add a client by handling a connection
        var clientTask = Task.Run(async () =>
        {
            await _service.HandleClientAsync(_webSocketMock.Object);
        });

        // Wait a bit for the client to be added
        await Task.Delay(100);

        // Change client state to closed and make SendAsync throw
        _webSocketMock.Setup(ws => ws.State).Returns(WebSocketState.Closed);
        _webSocketMock
            .Setup(ws => ws.SendAsync(It.IsAny<ArraySegment<byte>>(), WebSocketMessageType.Text, true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new WebSocketException());

        // Act
        await _service.BroadcastAsync(message);

        // Complete the client handling task
        messageReceived.SetResult(true);
        await clientTask;

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Removed dead WebSocket client")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Dispose_ClosesAllConnections()
    {
        // Arrange
        _webSocketMock.Setup(ws => ws.State).Returns(WebSocketState.Open);
        
        // Act
        _service.Dispose();

        // Assert
        _webSocketMock.Verify(
            ws => ws.CloseOutputAsync(
                WebSocketCloseStatus.NormalClosure,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never); // Since we haven't added any clients
    }
} 