using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CursorMCPMonitor.Services;

/// <summary>
/// Service for managing WebSocket connections and broadcasting messages to connected clients.
/// </summary>
public class WebSocketService : IDisposable
{
    private readonly ConcurrentDictionary<string, WebSocket> _clients = new();
    private readonly ILogger<WebSocketService> _logger;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public WebSocketService(ILogger<WebSocketService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Adds a new WebSocket client connection.
    /// </summary>
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

    /// <summary>
    /// Broadcasts a message to all connected WebSocket clients.
    /// </summary>
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
    }
} 