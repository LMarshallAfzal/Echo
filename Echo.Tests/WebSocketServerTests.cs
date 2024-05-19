using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Echo;
using Xunit;

namespace Echo.Tests;

public class WebSocketServerTests
{
    [Fact]
    public async Task StartAsync_ListensOnSpecifiedUrl()
    {
        // Arrange
        string serverUrl = "http://localhost:8081/";
        string webSocketUrl = "ws://localhost:8081/";
        var server = new WebSocketServer(serverUrl);

        // Act
        var serverTask = Task.Run(() => server.StartAsync());
        await Task.Delay(1000);

        // Assert
        using (var client = new ClientWebSocket())
        {
            var uri = new Uri(webSocketUrl);
            var connectTask = client.ConnectAsync(uri, CancellationToken.None);

            Assert.True(connectTask.Wait(TimeSpan.FromSeconds(5)), "Failed to connect to the server.");
            Assert.Equal(WebSocketState.Open, client.State);

            // Close the WebSocket connection
            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }

        // Cleanup
        await server.StopAsync();

        await serverTask;
    }
}