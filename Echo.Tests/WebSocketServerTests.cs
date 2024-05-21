using System;
using System.Net;
using System.Net.WebSockets;
using System.Net.Sockets;
using System.Threading.Tasks;
using Echo;
using Xunit;

namespace Echo.Tests;

public class WebSocketServerTests: IClassFixture<WebSocketServerFixture>
{
    private WebSocketServer _server;
    private string _serverUrl;
    private string _webSocketUrl;

    public WebSocketServerTests(WebSocketServerFixture fixture)
    {
        _server = fixture.Server;
        _serverUrl = fixture.ServerUrl;
        _webSocketUrl = _serverUrl.Replace("http", "ws");
    }

    [Fact]
    public async Task StartAsync_ListensOnSpecifiedUrl()
    {
        // Act
        var serverTask = _server.StartAsync();

        using (ClientWebSocket client = new())
        {
            // Arrange
            Uri uri = new(_webSocketUrl);
            Task connectTask = client.ConnectAsync(uri, CancellationToken.None);

            // Assert
            Assert.True(connectTask.Wait(TimeSpan.FromSeconds(5)), "Failed to connect to the server.");
            Assert.Equal(WebSocketState.Open, client.State);

            // Close the WebSocket connection
            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }
    }

    [Fact]
    public async Task ProcessWebSocketRequest_ValidRequest_AcceptsConnection()
    {
        // Act
        var serverTask = _server.StartAsync();

        using (ClientWebSocket client = new())
        {
            // Arrange
            Uri uri = new(_webSocketUrl);
            var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await client.ConnectAsync(uri, tokenSource.Token);

            // Assert
            Assert.Equal(WebSocketState.Open, client.State);

            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }
    }

    [Fact]
    public async Task ProcessWebSocketRequest_InvalidRequest_ReturnsStatusCodeBadRequest()
    {
        // Act
        var serverTask = _server.StartAsync();

        using (HttpClient client = new())
        {   
            // Arrange
            var response = await client.GetAsync(_webSocketUrl);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}