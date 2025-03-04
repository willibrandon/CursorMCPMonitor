using System.Net.WebSockets;

namespace CursorMCPMonitor.Interfaces;

public interface IWebSocketService : IDisposable
{
    Task HandleClientAsync(WebSocket webSocket);
    Task BroadcastAsync<T>(T message);
} 