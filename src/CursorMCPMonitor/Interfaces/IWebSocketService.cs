using System.Net.WebSockets;

namespace CursorMCPMonitor.Interfaces;

/// <summary>
/// Represents a WebSocket service that handles client connections and message broadcasting.
/// </summary>
/// <remarks>
/// This service manages WebSocket connections and provides methods for handling individual
/// client connections and broadcasting messages to all connected clients.
/// </remarks>
public interface IWebSocketService : IDisposable
{
    /// <summary>
    /// Handles communication with a connected WebSocket client.
    /// </summary>
    /// <param name="webSocket">The WebSocket instance representing the client connection.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// This method is responsible for managing the lifecycle of a client connection,
    /// including receiving messages and maintaining the connection until it is closed.
    /// </remarks>
    Task HandleClientAsync(WebSocket webSocket);

    /// <summary>
    /// Broadcasts a message to all connected WebSocket clients.
    /// </summary>
    /// <typeparam name="T">The type of the message to broadcast.</typeparam>
    /// <param name="message">The message to send to all connected clients.</param>
    /// <returns>A task that represents the asynchronous broadcast operation.</returns>
    /// <remarks>
    /// The message will be serialized and sent to all currently connected clients.
    /// If a client connection fails during broadcast, that client will be removed
    /// from the active connections list.
    /// </remarks>
    Task BroadcastAsync<T>(T message);
}
