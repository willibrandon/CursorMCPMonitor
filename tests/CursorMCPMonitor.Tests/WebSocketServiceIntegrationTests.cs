using CursorMCPMonitor.Interfaces;
using CursorMCPMonitor.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace CursorMCPMonitor.Tests;

public class WebSocketServiceIntegrationTests : IDisposable
{
    private readonly TestServer _server;
    private readonly IWebSocketService _webSocketService;
    private readonly List<WebSocket> _clients = new();

    public WebSocketServiceIntegrationTests()
    {
        // Set up test server
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging(logging => logging.AddConsole());
                services.AddSingleton<IWebSocketService, WebSocketService>();
            })
            .Configure(app =>
            {
                app.UseWebSockets();
                app.Map("/ws", builder =>
                {
                    builder.Run(async context =>
                    {
                        if (context.WebSockets.IsWebSocketRequest)
                        {
                            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                            var service = context.RequestServices.GetRequiredService<IWebSocketService>();
                            await service.HandleClientAsync(webSocket);
                        }
                        else
                        {
                            context.Response.StatusCode = 400;
                        }
                    });
                });
            });

        _server = new TestServer(builder);
        _webSocketService = _server.Services.GetRequiredService<IWebSocketService>();
    }

    public void Dispose()
    {
        // First, close all client connections
        foreach (var client in _clients)
        {
            try
            {
                if (client.State == WebSocketState.Open)
                {
                    // Use a short timeout to avoid hanging
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                    client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", cts.Token)
                        .Wait(TimeSpan.FromSeconds(1));
                }
            }
            catch { }
            finally
            {
                client.Dispose();
            }
        }

        _clients.Clear();

        // Then dispose the server
        try
        {
            _server?.Dispose();
        }
        catch { }

        GC.SuppressFinalize(this);
    }

    private async Task<WebSocket> ConnectWebSocketAsync()
    {
        var wsClient = _server.CreateWebSocketClient();
        var wsUri = new Uri(_server.BaseAddress, "ws");
        var client = await wsClient.ConnectAsync(wsUri, CancellationToken.None);
        _clients.Add(client);
        return client;
    }

    private async Task<string> ReceiveMessageAsync(WebSocket client)
    {
        var buffer = new byte[1024];
        var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        return Encoding.UTF8.GetString(buffer, 0, result.Count);
    }

    [Fact]
    public async Task BroadcastMessage_MultipleClients_AllClientsReceiveMessage()
    {
        // Arrange
        const int clientCount = 3;
        var clients = await Task.WhenAll(Enumerable.Range(0, clientCount).Select(_ => ConnectWebSocketAsync()));
        var message = new { text = "Test broadcast" };

        // Act
        await _webSocketService.BroadcastAsync(message);

        // Assert
        var receivedMessages = await Task.WhenAll(clients.Select(ReceiveMessageAsync));
        var expectedJson = JsonSerializer.Serialize(message);
        Assert.All(receivedMessages, msg => Assert.Equal(expectedJson, msg));
    }

    [Fact]
    public async Task ClientDisconnect_DuringBroadcast_OtherClientsStillReceiveMessages()
    {
        // Arrange
        var client1 = await ConnectWebSocketAsync();
        var client2 = await ConnectWebSocketAsync();
        var message = new { text = "Test broadcast" };

        // Abruptly close one client
        await client1.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Test disconnect", CancellationToken.None);

        // Act
        await _webSocketService.BroadcastAsync(message);

        // Assert
        var receivedMessage = await ReceiveMessageAsync(client2);
        var expectedJson = JsonSerializer.Serialize(message);
        Assert.Equal(expectedJson, receivedMessage);
    }

    [Fact]
    public async Task SlowClient_DoesNotBlockOtherClients()
    {
        // Arrange
        var normalClient = await ConnectWebSocketAsync();
        var slowClient = await ConnectWebSocketAsync();

        // Simulate a slow client by adding a delay to its receive operation
        var slowReceiveTask = Task.Run(async () =>
        {
            await Task.Delay(2000); // Simulate slow client
            return await ReceiveMessageAsync(slowClient);
        });

        var message = new { text = "Test broadcast" };
        var expectedJson = JsonSerializer.Serialize(message);

        // Act
        var broadcastTask = _webSocketService.BroadcastAsync(message);
        var normalClientReceiveTask = ReceiveMessageAsync(normalClient);

        // Assert - Normal client should receive message quickly
        var normalClientMessage = await Task.WhenAny(normalClientReceiveTask, Task.Delay(1000))
            .TimeoutAfter(TimeSpan.FromSeconds(2))
            .ContinueWith(t => t.Result == normalClientReceiveTask ? normalClientReceiveTask.Result : null);

        Assert.NotNull(normalClientMessage);
        Assert.Equal(expectedJson, normalClientMessage);

        // Wait for slow client to finish
        var slowClientMessage = await slowReceiveTask;
        Assert.Equal(expectedJson, slowClientMessage);
    }
}

public static class TaskExtensions
{
    public static async Task<T> TimeoutAfter<T>(this Task<T> task, TimeSpan timeout)
    {
        if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
        {
            return await task;
        }
        throw new TimeoutException();
    }
} 