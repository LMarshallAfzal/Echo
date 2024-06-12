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

    [Fact]
    public async Task ProcessWebSocketRequest_SendAndReceiveMessages()
    {
        // Act
        var serverTask = _server.StartAsync();

        using (ClientWebSocket client = new())
        {
            // Arrange
            Uri uri = new(_webSocketUrl);
            await client.ConnectAsync(uri, CancellationToken.None);

            string[] expectedMessages = { "Welcome to Echo Chat!", "Hello, server!" };
            string[] messagesToSend = { "Hello, server!" };

            var buffers = messagesToSend.Select(msg => System.Text.Encoding.UTF8.GetBytes(msg)).ToArray();

            foreach (var buffer in buffers)
            {
                await client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }

            var receivedMessages = new List<string>();
            var receiveBuffer = new byte[1024];

            WebSocketReceiveResult result;
            do
            {
                result = await client.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                Assert.Equal(WebSocketMessageType.Text, result.MessageType);

                var receivedMessage = System.Text.Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);
                receivedMessages.Add(receivedMessage);
            } while (!result.EndOfMessage || receivedMessages.Count < expectedMessages.Length);

            // Assert
            Assert.Equal(expectedMessages, receivedMessages);

            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }
    }

    [Fact]
    public async Task ProcessWebSocketRequest_HandleCloseMessage()
    {
        // Act
        var serverTask = _server.StartAsync();

        using (ClientWebSocket client = new())
        {
            // Arrange
            Uri uri = new(_webSocketUrl);
            await client.ConnectAsync(uri, CancellationToken.None);

            // Send a text message
            var message = "Hello, server!";
            var buffer = System.Text.Encoding.UTF8.GetBytes(message);
            await client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);

            // Send a close message
            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);

            // Assert
            Assert.Equal(WebSocketState.Closed, client.State);
        }
    }
}