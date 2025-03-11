using CursorMCPMonitor.Interfaces;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace CursorMCPMonitor.Services;

/// <summary>
/// Implementation of the WebSocket service that manages client connections and message broadcasting.
/// </summary>
/// <remarks>
/// This implementation uses a <see cref="ConcurrentDictionary{TKey,TValue}"/> to track active connections
/// and provides thread-safe operations for managing client connections and message broadcasting.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="WebSocketService"/> class.
/// </remarks>
/// <param name="logger">The logger instance for recording service operations.</param>
public class WebSocketService(ILogger<WebSocketService> logger) : IWebSocketService
{
    private readonly ConcurrentDictionary<string, WebSocket> _clients = new();
    private readonly ILogger<WebSocketService> _logger = logger;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    /// <inheritdoc />
    public async Task HandleClientAsync(WebSocket webSocket)
    {
        var clientId = Guid.NewGuid().ToString();
        _clients.TryAdd(clientId, webSocket);
        _logger.LogInformation("New WebSocket client connected: {ClientId}", clientId);

        try
        {
            // Keep connection alive until client disconnects
            var buffer = new byte[1024];
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    _cancellationTokenSource.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
            }
        }
        catch (WebSocketException ex)
        {
            _logger.LogWarning(ex, "WebSocket error for client {ClientId}", clientId);
        }
        finally
        {
            _clients.TryRemove(clientId, out _);
            _logger.LogInformation("WebSocket client disconnected: {ClientId}", clientId);
            
            if (webSocket.State == WebSocketState.Open)
            {
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Closing",
                    _cancellationTokenSource.Token);
            }
        }
    }

    /// <inheritdoc />
    public async Task BroadcastAsync<T>(T message)
    {
        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        var arraySegment = new ArraySegment<byte>(bytes);

        var deadSockets = new List<string>();

        foreach (var client in _clients)
        {
            try
            {
                if (client.Value.State == WebSocketState.Open)
                {
                    await client.Value.SendAsync(
                        arraySegment,
                        WebSocketMessageType.Text,
                        true,
                        _cancellationTokenSource.Token);
                }
                else
                {
                    deadSockets.Add(client.Key);
                }
            }
            catch (WebSocketException)
            {
                deadSockets.Add(client.Key);
            }
        }

        // Cleanup any dead connections
        foreach (var id in deadSockets)
        {
            _clients.TryRemove(id, out _);
            _logger.LogInformation("Removed dead WebSocket client: {ClientId}", id);
        }
    }

    /// <summary>
    /// Disposes of the WebSocket service, closing all client connections and releasing resources.
    /// </summary>
    /// <remarks>
    /// This implementation:
    /// <list type="bullet">
    /// <item><description>Cancels any pending operations</description></item>
    /// <item><description>Gracefully closes all open client connections</description></item>
    /// <item><description>Cleans up the client collection</description></item>
    /// <item><description>Disposes of the cancellation token source</description></item>
    /// </list>
    /// </remarks>
    public void Dispose()
    {
        try
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }
            
            // Close all WebSocket connections
            foreach (var client in _clients.Values)
            {
                try
                {
                    if (client.State == WebSocketState.Open)
                    {
                        client.CloseOutputAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Server shutting down",
                            CancellationToken.None).Wait(1000);
                    }
                }
                catch { }
                finally
                {
                    client.Dispose();
                }
            }

            _clients.Clear();
        }
        finally
        {
            _cancellationTokenSource.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
